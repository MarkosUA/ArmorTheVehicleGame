using System;
using System.Threading;
using UnityEngine;
using ArmorTheVehicle.Config;
using Random = UnityEngine.Random;

namespace ArmorTheVehicle.Enemy
{
    /// Owns the enemy's death visuals (collider/healthbar/particles/death-animation variant)
    /// and the delayed despawn that follows.
    internal sealed class EnemyDeathSequence
    {
        private const int DeathVariantCount = 4; // matches Death_0..Death_3 in EnemyAnimator.controller

        private readonly LevelConfig _config;
        private readonly EnemyAnimatorController _animView;

        private BoxCollider _collider;
        private GameObject _model;
        private GameObject _healthBar;
        private ParticleSystem _deathParticles;

        public EnemyDeathSequence(LevelConfig config, EnemyAnimatorController animView)
        {
            _config = config;
            _animView = animView;
        }

        public void Configure(BoxCollider collider, GameObject model, GameObject healthBar, ParticleSystem deathParticles)
        {
            _collider = collider;
            _model = model;
            _healthBar = healthBar;
            _deathParticles = deathParticles;
        }

        /// Plays the death visuals immediately, then waits out the despawn delay before
        /// deactivating the model/GameObject. `token` is the caller's per-spawn-generation
        /// cancellation token — if the enemy is reset/reused before the wait elapses, this
        /// returns without deactivating the (now-reused) instance out from under it.
        public async Awaitable PlayAndDespawn(GameObject enemyGameObject, CancellationToken token)
        {
            if (_collider != null) _collider.enabled = false;
            if (_healthBar != null) _healthBar.SetActive(false);
            if (_deathParticles != null) _deathParticles.Play();

            int deathIndex = Random.Range(0, DeathVariantCount);
            _animView.TriggerDeath(deathIndex);

            try
            {
                await Awaitable.WaitForSecondsAsync(_config.enemyDeathDespawnDelay, token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (_model != null) _model.SetActive(false);
            enemyGameObject.SetActive(false);
        }
    }
}
