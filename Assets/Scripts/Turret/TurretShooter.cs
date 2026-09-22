using UnityEngine;
using VContainer;
using ArmorTheVehicle.Config;
using ArmorTheVehicle.Core;
using ArmorTheVehicle.Projectile;
using ArmorTheVehicle.Audio;

namespace ArmorTheVehicle.Turret
{
    /// Auto-fires continuously from the muzzle whenever the car is running, and draws
    /// a constantly visible red aiming laser beam showing where projectiles fly.
    public sealed class TurretShooter : MonoBehaviour
    {
        [SerializeField] private LevelConfig _config;
        [SerializeField] private Transform _muzzle;
        [SerializeField] private ProjectilePool _projectilePool;
        [SerializeField] private LineRenderer _laserLine;

        [Header("Layers (defaults to Vehicle + Projectile — no need to change unless the project's layers change)")]
        [SerializeField] private LayerMask _laserIgnoreMask = (1 << 8) | (1 << 9);

        private GameState _gameState;
        private AudioManager _audioManager;
        private float _cooldownRemaining;
        private const float MaxLaserDistance = 40f;
        private const float MinFireRate = 0.01f; // guards against a zero/near-zero configured fire rate

        [Inject]
        public void Construct(GameState gameState, AudioManager audioManager)
        {
            _gameState = gameState;
            _audioManager = audioManager;
        }

        private void Awake()
        {
            if (_laserLine != null)
            {
                _laserLine.positionCount = 2;
                _laserLine.useWorldSpace = true;
                _laserLine.enabled = false;
            }
        }

        private void Update()
        {
            // Laser visibility is fully owned by UpdateLaser() (runs every LateUpdate
            // regardless of what happens here), so this only needs to gate firing.
            if (_gameState == null || !_gameState.IsPlayable) return;

            TickFireCooldown();
        }

        private void TickFireCooldown()
        {
            _cooldownRemaining -= Time.deltaTime;
            if (_cooldownRemaining <= 0f)
            {
                _cooldownRemaining = 1f / Mathf.Max(MinFireRate, _config.fireRate); // seconds-per-shot = 1 / shots-per-second
                Fire();
            }
        }

        private void LateUpdate()
        {
            UpdateLaser();
        }

        private void UpdateLaser()
        {
            if (_laserLine == null || _muzzle == null) return;

            if (_gameState == null || !_gameState.IsPlayable)
            {
                if (_laserLine.enabled) _laserLine.enabled = false;
                return;
            }

            if (!_laserLine.enabled) _laserLine.enabled = true;

            Vector3 startPos = _muzzle.position;
            Vector3 fireDir = _muzzle.forward;

            Vector3 endPos;

            if (Physics.Raycast(startPos, fireDir, out RaycastHit hit, MaxLaserDistance, ~_laserIgnoreMask, QueryTriggerInteraction.Collide))
            {
                endPos = hit.point;
            }
            else
            {
                endPos = startPos + fireDir * MaxLaserDistance;
            }

            _laserLine.SetPosition(0, startPos);
            _laserLine.SetPosition(1, endPos);
        }

        private void Fire()
        {
            if (_muzzle == null || _projectilePool == null) return;

            Vector3 muzzlePosition = _muzzle.position;
            Vector3 fireDirection = _muzzle.forward;

            _projectilePool.Rent(muzzlePosition, fireDirection, _config.projectileSpeed, _config.projectileDamage, _config.projectileLifetime);
            _audioManager?.PlayShoot();
        }
    }
}

