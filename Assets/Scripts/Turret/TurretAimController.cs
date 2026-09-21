using UnityEngine;
using VContainer;
using ArmorTheVehicle.Config;
using ArmorTheVehicle.Car;
using ArmorTheVehicle.Core;

namespace ArmorTheVehicle.Turret
{
    /// Rotates the turret toward the player's aim input (yaw) and pitches toward a fixed default combat point.
    public sealed class TurretAimController : MonoBehaviour
    {
        [SerializeField] private LevelConfig _config;
        [SerializeField] private CarInput _input;
        [SerializeField] private Transform _pivot;
        [SerializeField] private Transform _muzzle;

        private GameState _gameState;
        private float _currentLocalAngle;
        private float _currentPitch;

        private const float DefaultCombatDistance = 20f;
        private const float DefaultTargetHeight = 0.9f;
        private const float MinPitch = -5f;
        private const float MaxPitch = 25f;
        private const float MinHorizontalDistance = 0.1f; // avoids a divide-by-zero when computing pitch straight down
        private const float MinScreenWidth = 1f; // avoids a divide-by-zero when normalizing touch X against screen width

        [Inject]
        public void Construct(GameState gameState)
        {
            _gameState = gameState;
        }

        private void Awake()
        {
            if (_pivot == null) _pivot = transform;
        }

        private void Update()
        {
            // Freeze aiming (hold last angle) while the run isn't active or a menu (pause,
            // win/lose) is covering the screen — the turret shouldn't keep swiveling toward
            // touch input the player can no longer see land.
            if (_gameState == null || !_gameState.IsPlayable) return;

            UpdateYaw();
            UpdatePitch();

            _pivot.localRotation = Quaternion.Euler(_currentPitch, _currentLocalAngle, 0f);
        }

        // Horizontal aim, driven directly by player input.
        private void UpdateYaw()
        {
            float targetAngle = _input.IsAiming
                ? Mathf.Lerp(_config.turretMinAngle, _config.turretMaxAngle,
                    Mathf.Clamp01(_input.AimPoint.x / Mathf.Max(MinScreenWidth, Screen.width)))
                : 0f;

            _currentLocalAngle = Mathf.MoveTowardsAngle(
                _currentLocalAngle, targetAngle, _config.turretRotationSpeed * Time.deltaTime);
        }

        // Vertical aim, always settling toward a fixed point along the current yaw
        // direction at default combat height — see the class doc comment.
        private void UpdatePitch()
        {
            Vector3 muzzlePos = _muzzle != null ? _muzzle.position : _pivot.position;
            Vector3 carForward = _pivot.parent != null ? _pivot.parent.forward : Vector3.forward;
            Vector3 aimHorizontalDir = Quaternion.Euler(0f, _currentLocalAngle, 0f) * carForward;
            aimHorizontalDir.y = 0f;
            aimHorizontalDir.Normalize();

            Vector3 targetPoint = muzzlePos + aimHorizontalDir * DefaultCombatDistance;
            targetPoint.y = DefaultTargetHeight;

            Vector3 toTarget = targetPoint - muzzlePos;
            float horizontalDist = Mathf.Max(MinHorizontalDistance, new Vector2(toTarget.x, toTarget.z).magnitude);
            float targetPitch = -Mathf.Atan2(toTarget.y, horizontalDist) * Mathf.Rad2Deg;
            targetPitch = Mathf.Clamp(targetPitch, MinPitch, MaxPitch);

            _currentPitch = Mathf.MoveTowardsAngle(_currentPitch, targetPitch, _config.turretRotationSpeed * Time.deltaTime);
        }
    }
}

