using System.Collections.Generic;
using UnityEngine;
using VContainer;
using ArmorTheVehicle.Config;
using ArmorTheVehicle.Core;
using ArmorTheVehicle.Combat;
using ArmorTheVehicle.Audio;

namespace ArmorTheVehicle.Enemy
{
    /// Places LevelConfig.enemyCount enemies at random, minimally-spaced positions along
    /// the level. Reuses existing instances on restart (reposition + reset) instead of
    /// destroy/instantiate churn, so no event subscriptions leak across restarts.
    public sealed class EnemySpawner : MonoBehaviour
    {
        private const int MaxPlacementAttempts = 20;

        [SerializeField] private LevelConfig _config;
        [SerializeField] private EnemyAI _enemyPrefab;
        [SerializeField] private Transform _carTarget;
        [SerializeField] private Health _carHealth;
        [SerializeField] private Transform _spawnRoot;

        private GameState _gameState;
        private AudioManager _audioManager;
        private readonly List<EnemyAI> _enemies = new();
        private readonly List<Vector3> _spawnPositions = new();

        [Inject]
        public void Construct(GameState gameState, AudioManager audioManager)
        {
            _gameState = gameState;
            _audioManager = audioManager;
        }

        private void OnEnable() => _gameState.OnRestartRequested += SpawnAll;
        private void OnDisable() => _gameState.OnRestartRequested -= SpawnAll;

        private void Start()
        {
            SpawnAll();
        }

        private void SpawnAll()
        {
            _spawnPositions.Clear();

            for (int i = 0; i < _config.enemyCount; i++)
            {
                if (!TryPickPosition(out Vector3 position)) continue;

                _spawnPositions.Add(position);

                EnemyAI enemy = i < _enemies.Count ? _enemies[i] : Instantiate(_enemyPrefab, _spawnRoot);
                if (i >= _enemies.Count) _enemies.Add(enemy);

                enemy.transform.SetPositionAndRotation(position, Quaternion.identity);
                enemy.Init(_carTarget, _carHealth, _gameState, _audioManager);
            }

            for (int i = _config.enemyCount; i < _enemies.Count; i++)
            {
                _enemies[i].gameObject.SetActive(false);
            }
        }

        private bool TryPickPosition(out Vector3 position)
        {
            for (int attempt = 0; attempt < MaxPlacementAttempts; attempt++)
            {
                float z = Random.Range(_config.spawnMargin, _config.levelLength - _config.spawnMargin);
                float x = Random.Range(-_config.roadHalfWidth, _config.roadHalfWidth);
                var candidate = new Vector3(x, 0f, z);

                if (IsFarEnoughFromExisting(candidate))
                {
                    position = candidate;
                    return true;
                }
            }

            position = default;
            return false;
        }

        private bool IsFarEnoughFromExisting(Vector3 candidate)
        {
            foreach (Vector3 existing in _spawnPositions)
            {
                if (Vector3.Distance(existing, candidate) < _config.minEnemySpacing)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
