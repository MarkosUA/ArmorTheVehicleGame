using UnityEngine;
using VContainer;
using ArmorTheVehicle.Core;

namespace ArmorTheVehicle.CameraRig
{
    /// Smoothed behind/above chase camera. Runs in LateUpdate, after the car's FixedUpdate
    /// motion has been applied, which is what keeps this jitter-free.
    public sealed class ChaseCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private Vector3 _offset = new(0f, 4f, -6f);
        [SerializeField] private float _positionSmoothTime = 0.15f;
        [SerializeField] private float _rotationSmoothSpeed = 6f;
        [SerializeField] private float _lookAheadDistance = 4f;

        private GameState _gameState;
        private Vector3 _velocity;

        [Inject]
        public void Construct(GameState gameState)
        {
            _gameState = gameState;
        }

        private void LateUpdate()
        {
            if (_target == null) return;
            // Freeze only while an actual pause menu is up — never on "not running" alone,
            // since that window also covers the moment right after a restart where
            // CarController snaps the car back to its start position; the camera needs to
            // keep tracking through that snap (and the pre-first-tap idle framing) so both
            // visibly reset together instead of leaving the camera stranded at its old spot.
            if (_gameState != null && _gameState.IsPaused) return;

            Vector3 desiredPosition = _target.TransformPoint(_offset);
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _velocity, _positionSmoothTime);

            Vector3 lookPoint = _target.position + _target.forward * _lookAheadDistance;
            Vector3 lookDirection = lookPoint - transform.position;
            if (lookDirection.sqrMagnitude < 0.0001f) return;

            Quaternion desiredRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, _rotationSmoothSpeed * Time.deltaTime);
        }
    }
}
