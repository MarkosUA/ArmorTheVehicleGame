using System;
using UnityEngine;
using ArmorTheVehicle.Combat;

namespace ArmorTheVehicle.Projectile
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Projectile : MonoBehaviour
    {
        private Rigidbody _rigidbody;
        private TrailRenderer _trailRenderer;
        private Vector3 _direction;
        private float _speed;
        private float _damage;
        private float _lifeRemaining;
        private Action<Projectile> _onExpired;

        [Header("Layers (defaults to Vehicle — no need to change unless the project's layers change)")]
        [SerializeField] private LayerMask _vehicleLayer = 1 << 8;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.isKinematic = true;
            _trailRenderer = GetComponent<TrailRenderer>();
        }

        public void Launch(Vector3 position, Vector3 direction, float speed, float damage, float lifetime, Action<Projectile> onExpired)
        {
            Quaternion rotation = Quaternion.LookRotation(direction);
            transform.SetPositionAndRotation(position, rotation);
            _rigidbody.position = position;
            _rigidbody.rotation = rotation;

            if (_trailRenderer != null)
            {
                _trailRenderer.Clear();
            }

            _direction = direction;
            _speed = speed;
            _damage = damage;
            _lifeRemaining = lifetime;
            _onExpired = onExpired;
        }

        private void FixedUpdate()
        {
            Vector3 next = _rigidbody.position + _direction * (_speed * Time.fixedDeltaTime);
            _rigidbody.MovePosition(next);

            _lifeRemaining -= Time.fixedDeltaTime;
            if (_lifeRemaining <= 0f)
            {
                Expire();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // The car's collider is on the Vehicle layer, so this alone excludes it —
            // no need to also walk the hierarchy looking for a CarController.
            if ((_vehicleLayer.value & (1 << other.gameObject.layer)) != 0) return;

            if (other.TryGetComponent(out IDamageable damageable) && damageable.IsAlive)
            {
                damageable.TakeDamage(_damage);
                Expire();
            }
        }

        private void Expire()
        {
            _onExpired?.Invoke(this);
        }
    }
}

