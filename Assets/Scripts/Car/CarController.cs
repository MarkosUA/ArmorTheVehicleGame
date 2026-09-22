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

        [Header("Visuals (optional — auto-located by name/search under \"Model\" if left empty)")]
        [SerializeField] private Transform _model;
        [SerializeField] private Transform[] _wheels;
        [SerializeField] private Renderer[] _flashRenderers;

        private GameState _gameState;
        private Rigidbody _rigidbody;
        private Health _health;

        private CarWheelSpinner _wheelSpinner;
        private HitFlash _hitFlash;
        private ScalePulse _scalePulse;

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

            Transform model = ResolveModel();

            _wheelSpinner = CarWheelSpinner.FromModel(model, _wheels, _config.carWheelRadius);
            _hitFlash = HitFlash.FromModel(model, _flashRenderers);
            _scalePulse = new ScalePulse(model, _config.carHitPulseScale);
        }

        // Prefers the Inspector-assigned reference; falls back to a by-name lookup for as
        // long as _model is left unassigned. The wheels live inside a nested FBX-model
        // prefab instance with no stable fileIDs to reference from outside it directly, so
        // this fallback is what makes the feature work out of the box — assigning _model
        // (and _wheels) by hand later just skips it.
        private Transform ResolveModel() => _model != null ? _model : transform.Find("Model");

        private void Start()
        {
            _health.Configure(_config.carMaxHealth);
        }

        private void OnEnable()
        {
            _input.OnTap += HandleTap;
            _health.OnDied += HandleDied;
            _health.OnDamaged += HandleDamaged;
            _gameState.OnRestartRequested += HandleRestart;
        }

        private void OnDisable()
        {
            _input.OnTap -= HandleTap;
            _health.OnDied -= HandleDied;
            _health.OnDamaged -= HandleDamaged;
            _gameState.OnRestartRequested -= HandleRestart;
        }

        private void FixedUpdate()
        {
            if (!_gameState.IsPlayable) return;

            _speedT = Mathf.Min(1f, _speedT + Time.fixedDeltaTime / Mathf.Max(0.01f, _config.carStartEaseDuration));
            float speed = Mathf.SmoothStep(0f, _config.carForwardSpeed, _speedT);

            Vector3 nextPosition = _rigidbody.position + transform.forward * (speed * Time.fixedDeltaTime);
            _rigidbody.MovePosition(nextPosition);
            _wheelSpinner.Tick(speed, Time.fixedDeltaTime);

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

        private void HandleDamaged(float current, float max)
        {
            _hitFlash.Flash();
            _scalePulse.Pulse();
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
