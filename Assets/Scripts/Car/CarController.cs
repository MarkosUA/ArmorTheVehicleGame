using UnityEngine;
using VContainer;
using ArmorTheVehicle.Config;
using ArmorTheVehicle.Core;
using ArmorTheVehicle.Combat;

namespace ArmorTheVehicle.Car
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Health))]
    public sealed class CarController : MonoBehaviour
    {
        [SerializeField] private LevelConfig _config;
        [SerializeField] private CarInput _input;

        private GameState _gameState;
        private Rigidbody _rigidbody;
        private Health _health;

        private Vector3 _startPosition;
        private Quaternion _startRotation;
        private float _speedT;

        [Inject]
        public void Construct(GameState gameState)
        {
            _gameState = gameState;
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _health = GetComponent<Health>();
            _startPosition = transform.position;
            _startRotation = transform.rotation;
        }

        private void Start()
        {
            _health.Configure(_config.carMaxHealth);
        }

        private void OnEnable()
        {
            _input.OnTap += HandleTap;
            _health.OnDied += HandleDied;
            _gameState.OnRestartRequested += HandleRestart;
        }

        private void OnDisable()
        {
            _input.OnTap -= HandleTap;
            _health.OnDied -= HandleDied;
            _gameState.OnRestartRequested -= HandleRestart;
        }

        private void FixedUpdate()
        {
            if (!_gameState.IsPlayable) return;

            _speedT = Mathf.Min(1f, _speedT + Time.fixedDeltaTime / Mathf.Max(0.01f, _config.carStartEaseDuration));
            float speed = Mathf.SmoothStep(0f, _config.carForwardSpeed, _speedT);

            Vector3 nextPosition = _rigidbody.position + transform.forward * (speed * Time.fixedDeltaTime);
            _rigidbody.MovePosition(nextPosition);

            if (nextPosition.z >= _config.levelLength)
            {
                _gameState.ReportWin();
            }
        }

        private void HandleTap()
        {
            if (!_gameState.IsRunning && !_gameState.HasWon && !_gameState.HasLost)
            {
                _gameState.StartRun();
            }
        }

        private void HandleDied()
        {
            _gameState.ReportLoss();
        }

        private void HandleRestart()
        {
            _speedT = 0f;
            _rigidbody.position = _startPosition;
            _rigidbody.rotation = _startRotation;
            _health.ResetHealth();
        }
    }
}
