using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace ArmorTheVehicle.Projectile
{
    /// Thin wrapper over the built-in UnityEngine.Pool.ObjectPool — no hand-rolled pooling.
    public sealed class ProjectilePool : MonoBehaviour
    {
        [SerializeField] private Projectile _prefab;
        [SerializeField] private int _defaultCapacity = 16;
        [SerializeField] private int _maxCapacity = 64;

        private ObjectPool<Projectile> _pool;
        private readonly HashSet<Projectile> _active = new();

        private void Awake()
        {
            _pool = new ObjectPool<Projectile>(
                createFunc: () => Instantiate(_prefab, transform),
                actionOnGet: p => p.gameObject.SetActive(true),
                actionOnRelease: p => p.gameObject.SetActive(false),
                actionOnDestroy: p => Destroy(p.gameObject),
                defaultCapacity: _defaultCapacity,
                maxSize: _maxCapacity);
        }

        public Projectile Rent(Vector3 position, Vector3 direction, float speed, float damage, float lifetime)
        {
            Projectile projectile = _pool.Get();
            _active.Add(projectile);
            projectile.Launch(position, direction, speed, damage, lifetime, Release);
            return projectile;
        }

        public void ReleaseAll()
        {
            foreach (Projectile projectile in new List<Projectile>(_active))
            {
                Release(projectile);
            }
        }

        private void Release(Projectile projectile)
        {
            if (_active.Remove(projectile))
            {
                _pool.Release(projectile);
            }
        }
    }
}
