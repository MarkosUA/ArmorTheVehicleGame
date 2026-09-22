using UnityEngine;
using ArmorTheVehicle.Config;
using ArmorTheVehicle.Combat;
using ArmorTheVehicle.Core;
using ArmorTheVehicle.Audio;

namespace ArmorTheVehicle.Enemy
{
    /// Idle / Wandering -> Chasing (Running) -> Attacking -> Dead.
    /// Manages state transitions, animations (Idle, IsWalking, IsRunning, Attack, Death),
    /// and kamikaze detonation when reaching the vehicle.
    [RequireComponent(typeof(Health))]
    public sealed class EnemyAI : MonoBehaviour
    {
        private enum State { Idle, Wandering, Chasing, Attacking, Dead }

        private const float MinRotationThresholdSqr = 0.001f; // below this, toTarget is too close to zero to derive a facing direction from
        private const int DeathVariantCount = 4; // matches Death_0..Death_3 in EnemyAnimator.controller

        [SerializeField] private LevelConfig _config;
        [SerializeField] private Animator _animator;
        [SerializeField] private GameObject _model;
        [SerializeField] private GameObject _healthBar;
        [SerializeField] private ParticleSystem _deathParticles;
        [SerializeField] private Renderer[] _flashRenderers;

        private Health _health;
        private BoxCollider _collider;
        private Transform _target;
        private Collider _targetCollider;
        private IDamageable _targetDamageable;
        private GameState _gameState;
        private AudioManager _audioManager;
        private State _state;

        private EnemyAnimatorController _animView;
        private EnemyWanderer _wanderer;
        private HitFlash _hitFlash;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _collider = GetComponent<BoxCollider>();
            _animView = new EnemyAnimatorController(_animator);
            _wanderer = new EnemyWanderer(_config);
            _hitFlash = HitFlash.FromModel(_model != null ? _model.transform : null, _flashRenderers);
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
        }

        private void HandleDamaged(float current, float max)
        {
            _hitFlash.Flash();
            _audioManager?.PlayEnemyHit();
        }

        public void Init(Transform target, IDamageable targetDamageable, GameState gameState, AudioManager audioManager)
        {
            _target = target;
            _targetCollider = target != null ? target.GetComponent<Collider>() : null;
            _targetDamageable = targetDamageable;
            _gameState = gameState;
            _audioManager = audioManager;
            ResetForSpawn();
        }

        public void ResetForSpawn()
        {
            _state = State.Idle;
            _health.Configure(_config.enemyMaxHealth);
            _wanderer.ResetForSpawn(transform.position);

            if (_collider != null) _collider.enabled = true;
            if (_model != null) _model.SetActive(true);
            if (_healthBar != null) _healthBar.SetActive(true);
            gameObject.SetActive(true);

            // Must run after the GameObject/model are active — Animator.Play() logs/throws
            // on an inactive hierarchy, which is exactly the state a pooled enemy is in
            // right after DespawnAfterDelay deactivated it, just before it's reused here.
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
                case State.Attacking:
                    TickAttacking();
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
                return;
            }

            if (_wanderer.TickMove(transform, Time.deltaTime))
            {
                // Reached destination, go back to idle
                _state = State.Idle;
                _animView.StopWalking();
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
            if (IsTargetInRange(toTarget))
            {
                DetonateKamikaze(toTarget);
                return;
            }

            Vector3 direction = toTarget.normalized;
            transform.position += direction * (_config.enemyMoveSpeed * Time.deltaTime);
            transform.rotation = Quaternion.LookRotation(direction);
        }

        private void TickAttacking()
        {
            DetonateKamikaze(FlatOffsetToTarget());
        }

        private bool IsTargetInRange(Vector3 toTarget)
        {
            // Measure from the target's actual collider surface, not just its pivot — for
            // a long, off-center collider like the car's, a side approach can be well
            // within attack reach of the nearest body panel while still being farther than
            // enemyAttackRange from the pivot itself. Without this, side attacks could
            // silently fail to register (relying only on physical OnTriggerEnter overlap,
            // which isn't guaranteed to land every frame).
            if (_targetCollider != null)
            {
                Vector3 closest = _targetCollider.ClosestPoint(transform.position);
                Vector3 offset = closest - transform.position;
                offset.y = 0f;
                return offset.magnitude <= _config.enemyAttackRange;
            }

            return toTarget.magnitude <= _config.enemyAttackRange;
        }

        private void OnTriggerEnter(Collider other) => TryDetonateFromContact(other);

        // Physical backstop alongside the distance check in TickChasing: OnTriggerEnter
        // only fires on the first overlapping frame, so relying on it alone risks missing
        // contact entirely if that single frame's timing lines up awkwardly (most visible
        // on a fast side approach, where the car sweeps past instead of driving straight
        // into the enemy). OnTriggerStay re-checks every physics step for as long as the
        // enemy and car colliders overlap, so as long as they ever touch at all, the attack
        // is guaranteed to fire — DetonateKamikaze's own state guard makes repeated calls
        // during the same overlap a safe no-op.
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

            // Direct reference comparison against the car's own collider (cached once in
            // Init) — no hierarchy walk needed, since the car only ever has this one collider.
            if (other == _targetCollider)
            {
                DetonateKamikaze(FlatOffsetToTarget());
            }
        }

        private void DetonateKamikaze(Vector3 toTarget)
        {
            // Also guards against re-entrant calls once already attacking: TickAttacking()
            // calls this every frame while _state == Attacking, and OnTriggerEnter can
            // re-fire on continued overlap.
            if (_state == State.Attacking || _state == State.Dead) return;

            _state = State.Attacking;

            if (toTarget.sqrMagnitude > MinRotationThresholdSqr)
            {
                transform.rotation = Quaternion.LookRotation(toTarget.normalized);
            }

            _animView.TriggerAttack();
            _audioManager?.PlayEnemyAttack();

            _targetDamageable?.TakeDamage(_config.enemyAttackDamage);
            SelfDestructAfterAttackAnim();
        }

        private async void SelfDestructAfterAttackAnim()
        {
            await Awaitable.WaitForSecondsAsync(_config.enemyAttackAnimationDuration);
            if (_state != State.Attacking) return; // reset by a restart, or already killed by another damage source mid-wait

            _health.TakeDamage(_health.MaxHealth);
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

            if (_collider != null) _collider.enabled = false;
            if (_healthBar != null) _healthBar.SetActive(false);
            if (_deathParticles != null) _deathParticles.Play();

            int deathIndex = Random.Range(0, DeathVariantCount);
            _animView.TriggerDeath(deathIndex);

            DespawnAfterDelay();
        }

        private async void DespawnAfterDelay()
        {
            await Awaitable.WaitForSecondsAsync(_config.enemyDeathDespawnDelay);
            if (_state != State.Dead) return; // already reset by a restart while the delay was pending

            if (_model != null) _model.SetActive(false);
            gameObject.SetActive(false);
        }
    }
}
