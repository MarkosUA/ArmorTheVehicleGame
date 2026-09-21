using UnityEngine;
using ArmorTheVehicle.Config;

namespace ArmorTheVehicle.Enemy
{
    /// Owns the "idle near spawn, occasionally wander to a nearby random point" behavior
    /// for an enemy that hasn't aggroed yet — timers, target-point picking, and the actual
    /// move-toward-target step. The caller (EnemyAI) still owns the state machine and
    /// decides when to start/stop wandering; this class only does the math.
    internal sealed class EnemyWanderer
    {
        private readonly LevelConfig _config;

        private Vector3 _spawnPosition;
        private Vector3 _wanderTarget;
        private float _wanderWaitTimer;
        private bool _canWander;

        public EnemyWanderer(LevelConfig config)
        {
            _config = config;
        }

        public void ResetForSpawn(Vector3 spawnPosition)
        {
            _spawnPosition = spawnPosition;
            _canWander = Random.value < _config.enemyWanderChance;
            _wanderWaitTimer = Random.Range(_config.enemyWanderIdleMinTime, _config.enemyWanderIdleMaxTime);
        }

        /// Ticks the idle-wait countdown; returns true once it's time to start wandering.
        public bool TickIdleWait(float deltaTime)
        {
            if (!_canWander) return false;

            _wanderWaitTimer -= deltaTime;
            return _wanderWaitTimer <= 0f;
        }

        public void BeginWander(Vector3 currentPosition)
        {
            Vector2 randomCircle = Random.insideUnitCircle * _config.enemyWanderRadius;
            float targetX = Mathf.Clamp(_spawnPosition.x + randomCircle.x, -_config.roadHalfWidth, _config.roadHalfWidth);
            float targetZ = Mathf.Clamp(_spawnPosition.z + randomCircle.y, 0f, _config.levelLength);
            _wanderTarget = new Vector3(targetX, currentPosition.y, targetZ);
        }

        /// Moves transform toward the current wander target. Returns true once the
        /// destination is reached (and re-rolls the next idle-wait timer internally).
        public bool TickMove(Transform transform, float deltaTime)
        {
            Vector3 offset = _wanderTarget - transform.position;
            offset.y = 0f;

            if (offset.magnitude <= 0.2f)
            {
                _wanderWaitTimer = Random.Range(_config.enemyWanderIdleMinTime, _config.enemyWanderIdleMaxTime);
                return true;
            }

            Vector3 direction = offset.normalized;
            transform.position += direction * (_config.enemyWalkSpeed * deltaTime);
            transform.rotation = Quaternion.LookRotation(direction);
            return false;
        }
    }
}
