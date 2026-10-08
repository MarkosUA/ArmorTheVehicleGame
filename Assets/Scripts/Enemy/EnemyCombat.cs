using System;
using System.Threading;
using UnityEngine;
using ArmorTheVehicle.Config;
using ArmorTheVehicle.Combat;
using ArmorTheVehicle.Audio;

namespace ArmorTheVehicle.Enemy
{
    /// Owns attack-range checking and the kamikaze attack itself: facing the target,
    /// triggering the attack animation/audio, dealing damage, then self-destructing once
    /// the attack animation has had time to play out.
    internal sealed class EnemyCombat
    {
        private const float MinRotationThresholdSqr = 0.001f; // below this, toTarget is too close to zero to derive a facing direction from

        private readonly LevelConfig _config;
        private readonly EnemyAnimatorController _animView;

        private Collider _targetCollider;
        private IDamageable _targetDamageable;
        private AudioManager _audioManager;

        public EnemyCombat(LevelConfig config, EnemyAnimatorController animView)
        {
            _config = config;
            _animView = animView;
        }

        public void Configure(Collider targetCollider, IDamageable targetDamageable, AudioManager audioManager)
        {
            _targetCollider = targetCollider;
            _targetDamageable = targetDamageable;
            _audioManager = audioManager;
        }

        public bool IsTargetCollider(Collider other) => other == _targetCollider;

        // Measures from the target's actual collider surface, not just its pivot — for a
        // long, off-center collider like the car's, a side approach can be well within
        // attack reach of the nearest body panel while still being farther than
        // enemyAttackRange from the pivot itself. Without this, side attacks could silently
        // fail to register (relying only on physical trigger overlap, which isn't
        // guaranteed to land every frame).
        public bool IsInRange(Transform self, Vector3 toTarget)
        {
            if (_targetCollider != null)
            {
                Vector3 closest = _targetCollider.ClosestPoint(self.position);
                Vector3 offset = closest - self.position;
                offset.y = 0f;
                return offset.magnitude <= _config.enemyAttackRange;
            }

            return toTarget.magnitude <= _config.enemyAttackRange;
        }

        /// Faces the target, plays the attack animation/audio, deals damage immediately,
        /// then waits out the attack animation before self-destructing. `token` is the
        /// caller's per-spawn-generation cancellation token — if the enemy is reset/reused
        /// before the wait elapses, this returns without dealing the follow-up self-damage.
        public async Awaitable Detonate(Transform self, Health selfHealth, Vector3 toTarget, CancellationToken token)
        {
            if (toTarget.sqrMagnitude > MinRotationThresholdSqr)
            {
                self.rotation = Quaternion.LookRotation(toTarget.normalized);
            }

            _animView.TriggerAttack();
            _audioManager?.PlayEnemyAttack();
            _targetDamageable?.TakeDamage(_config.enemyAttackDamage);

            try
            {
                await Awaitable.WaitForSecondsAsync(_config.enemyAttackAnimationDuration, token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            selfHealth.TakeDamage(selfHealth.MaxHealth);
        }
    }
}
