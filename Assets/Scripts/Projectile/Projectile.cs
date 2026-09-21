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
        private int _vehicleLayer;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.isKinematic = true;
            _trailRenderer = GetComponent<TrailRenderer>();
            _vehicleLayer = LayerMask.NameToLayer("Vehicle");
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
            if (other.gameObject.layer == _vehicleLayer) return;

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

