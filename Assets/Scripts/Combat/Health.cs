using System;
using UnityEngine;

namespace ArmorTheVehicle.Combat
{
    /// Shared HP component used by both the car and enemies — the single place damage/death logic lives.
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [SerializeField] private float _maxHealth = 100f;

        public float MaxHealth => _maxHealth;
        public float CurrentHealth { get; private set; }
        public bool IsAlive => CurrentHealth > 0f;

        // Fires only for genuine damage (TakeDamage) — this is what gameplay reactions like
        // a hit-flash or hit sound should subscribe to.
        public event Action<float, float> OnDamaged; // current, max
        // Fires for ANY change to CurrentHealth, damage or reset alike — this is what purely
        // visual health display (e.g. a health bar) should subscribe to, so it refills
        // correctly on respawn instead of staying at whatever it showed before.
        public event Action<float, float> OnHealthChanged; // current, max
        public event Action OnDied;

        private void Awake()
        {
            CurrentHealth = _maxHealth;
        }

        public void Configure(float maxHealth)
        {
            _maxHealth = maxHealth;
            ResetHealth();
        }

        public void ResetHealth()
        {
            CurrentHealth = _maxHealth;
            OnHealthChanged?.Invoke(CurrentHealth, _maxHealth);
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive || amount <= 0f) return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            OnDamaged?.Invoke(CurrentHealth, _maxHealth);
            OnHealthChanged?.Invoke(CurrentHealth, _maxHealth);

            if (CurrentHealth <= 0f)
            {
                OnDied?.Invoke();
            }
        }
    }
}
