using System.Threading;
using UnityEngine;
using ArmorTheVehicle.Config;
using ArmorTheVehicle.Combat;
using ArmorTheVehicle.Core;
using ArmorTheVehicle.Audio;

namespace ArmorTheVehicle.Enemy
{
    /// Idle / Wandering -> Chasing (Running) -> Attacking -> Dead.
    /// Owns the state machine and Unity lifecycle only — movement decisions dispatch to
    /// EnemyWanderer (wandering) or apply directly here (chasing); attack/damage and death/
    /// despawn are owned by EnemyCombat and EnemyDeathSequence respectively.
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class EnemyAI : MonoBehaviour
    {
        private enum State { Idle, Wandering, Chasing, Attacking, Dead }

        [SerializeField] private LevelConfig _config;
        [SerializeField] private Animator _animator;
        [SerializeField] private GameObject _model;
        [SerializeField] private GameObject _healthBar;
        [SerializeField] private ParticleSystem _deathParticles;
        [SerializeField] private Renderer[] _flashRenderers;

        private Health _health;
        private BoxCollider _collider;
        private Rigidbody _rigidbody;
        private Transform _target;
        private GameState _gameState;
        private AudioManager _audioManager;
        private State _state;

        private EnemyAnimatorController _animView;
        private EnemyWanderer _wanderer;
        private HitFlash _hitFlash;
        private EnemyCombat _combat;
        private EnemyDeathSequence _death;
        private CancellationTokenSource _lifecycleCts;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _collider = GetComponent<BoxCollider>();
            _rigidbody = GetComponent<Rigidbody>();
            _animView = new EnemyAnimatorController(_animator);
            _wanderer = new EnemyWanderer(_config);
            _hitFlash = HitFlash.FromModel(_model != null ? _model.transform : null, _flashRenderers);
            _combat = new EnemyCombat(_config, _animView);
            _death = new EnemyDeathSequence(_config, _animView);
        }

        private void OnEnable()
        {
            _health.OnDied += HandleDied;
            _health.OnDamaged += HandleDamaged;
        }

        private void OnDisable()
        {
            _health.OnDied -= HandleDied;
            _health.OnDamaged -= HandleDamaged;

            // Single choke point for every deactivation path (death despawn, and
            // EnemySpawner's excess-enemy shrink loop) — cancels any pending
            // attack-self-destruct or death-despawn wait so it can't wake up against a
            // deactivated, later-reused instance.
            _lifecycleCts?.Cancel();
            _lifecycleCts?.Dispose();
            _lifecycleCts = null;
        }

        private void HandleDamaged(float current, float max)
        {
            _hitFlash.Flash();
            _audioManager?.PlayEnemyHit();
        }

        public void Init(Transform target, IDamageable targetDamageable, GameState gameState, AudioManager audioManager)
        {
            _target = target;
            Collider targetCollider = target != null ? target.GetComponent<Collider>() : null;
            _gameState = gameState;
            _audioManager = audioManager;
            _combat.Configure(targetCollider, targetDamageable, audioManager);
            _death.Configure(_collider, _model, _healthBar, _deathParticles);
            ResetForSpawn();
        }

        public void ResetForSpawn()
        {
            _state = State.Idle;
            _health.Configure(_config.enemyMaxHealth);
            _wanderer.ResetForSpawn(transform.position);

            _lifecycleCts?.Cancel();
            _lifecycleCts?.Dispose();
            _lifecycleCts = new CancellationTokenSource();

            if (_collider != null) _collider.enabled = true;
            if (_model != null) _model.SetActive(true);
            if (_healthBar != null) _healthBar.SetActive(true);
            gameObject.SetActive(true);

            // Must run after the GameObject/model are active — Animator.Play() logs/throws
            // on an inactive hierarchy, which is exactly the state a pooled enemy is in
            // right after despawn deactivated it, just before it's reused here.
            _animView.ResetToRandomIdle();
        }

        private void Update()
        {
            // Do not move or act before the car starts moving, after the run ends, or while paused
            if (_gameState == null || !_gameState.IsPlayable)
            {
                _animView.FreezeMovement();
                return;
            }

            switch (_state)
            {
                case State.Idle:
                    TickIdle();
                    break;
                case State.Wandering:
                    TickWandering();
                    break;
                case State.Chasing:
                    TickChasing();
                    break;
            }
        }

        // Movement itself is applied here (fixed timestep, matching the car's own
        // Rigidbody-driven movement) rather than in Update() — trigger detection
        // (OnTriggerEnter/Stay) is evaluated on this same fixed-timestep physics pass, so
        // moving the collider here keeps its physics-tracked pose in sync with what the
        // triggers actually see, instead of lagging behind a variable-rate Update().
        private void FixedUpdate()
        {
            if (_gameState == null || !_gameState.IsPlayable) return;

            switch (_state)
            {
                case State.Wandering:
                    if (_wanderer.TickMove(_rigidbody, Time.fixedDeltaTime))
                    {
                        _state = State.Idle;
                        _animView.StopWalking();
                    }
                    break;
                case State.Chasing:
                    Vector3 direction = FlatOffsetToTarget().normalized;
                    _rigidbody.MovePosition(_rigidbody.position + direction * (_config.enemyMoveSpeed * Time.fixedDeltaTime));
                    _rigidbody.MoveRotation(Quaternion.LookRotation(direction));
                    break;
            }
        }

        private void TickIdle()
        {
            // Check aggro radius to target (car)
            if (FlatOffsetToTarget().magnitude <= _config.enemyAggroRadius)
            {
                StartChasing();
                return;
            }

            // Idle -> Wandering logic
            if (_wanderer.TickIdleWait(Time.deltaTime))
            {
                StartWandering();
            }
        }

        private void StartWandering()
        {
            _wanderer.BeginWander(transform.position);
            _state = State.Wandering;
            _animView.EnterWandering();
        }

        private void TickWandering()
        {
            // Check aggro radius while wandering
            if (FlatOffsetToTarget().magnitude <= _config.enemyAggroRadius)
            {
                StartChasing();
            }
        }

        private void StartChasing()
        {
            _state = State.Chasing;
            _animView.EnterChasing();
        }

        private void TickChasing()
        {
            Vector3 toTarget = FlatOffsetToTarget();
            if (_combat.IsInRange(transform, toTarget))
            {
                BeginAttack(toTarget);
            }
        }

        private void OnTriggerEnter(Collider other) => TryDetonateFromContact(other);

        // Physical backstop alongside the distance check in TickChasing: OnTriggerEnter
        // only fires on the first overlapping frame, so relying on it alone risks missing
        // contact entirely if that single frame's timing lines up awkwardly (most visible
        // on a fast side approach, where the car sweeps past instead of driving straight
        // into the enemy). OnTriggerStay re-checks every physics step for as long as the
        // enemy and car colliders overlap, so as long as they ever touch at all, the attack
        // is guaranteed to fire — BeginAttack's own state guard makes repeated calls during
        // the same overlap a safe no-op.
        private void OnTriggerStay(Collider other) => TryDetonateFromContact(other);

        private void TryDetonateFromContact(Collider other)
        {
            // Only Idle/Wandering/Chasing can still transition into an attack; once
            // Attacking or Dead the outcome is already decided, so bail before even the
            // (cheap) collider comparison below — this runs every physics step for as long
            // as the colliders overlap, which for a kamikaze enemy spans its whole
            // attack/death window.
            if (_state != State.Idle && _state != State.Wandering && _state != State.Chasing) return;
            if (_gameState == null || !_gameState.IsPlayable) return;

            if (_combat.IsTargetCollider(other))
            {
                BeginAttack(FlatOffsetToTarget());
            }
        }

        private void BeginAttack(Vector3 toTarget)
        {
            // Also guards against re-entrant calls once already attacking: TickChasing()
            // and TryDetonateFromContact() can both reach here on the same or later frames
            // while _state == Attacking.
            if (_state == State.Attacking || _state == State.Dead) return;

            _state = State.Attacking;
            _ = _combat.Detonate(transform, _health, toTarget, _lifecycleCts.Token);
        }

        private Vector3 FlatOffsetToTarget()
        {
            if (_target == null) return Vector3.zero;
            Vector3 offset = _target.position - transform.position;
            offset.y = 0f;
            return offset;
        }

        private void HandleDied()
        {
            _state = State.Dead;
            _ = _death.PlayAndDespawn(gameObject, _lifecycleCts.Token);
        }
    }
}
