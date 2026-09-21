using UnityEngine;

namespace ArmorTheVehicle.Enemy
{
    /// Thin wrapper over the enemy's Animator — owns every parameter/state hash and
    /// null-guards each call, since _animator may be left unassigned in the Inspector.
    internal sealed class EnemyAnimatorController
    {
        private static readonly int AnimIsWalking = Animator.StringToHash("IsWalking");
        private static readonly int AnimIsRunning = Animator.StringToHash("IsRunning");
        private static readonly int AnimAttack = Animator.StringToHash("Attack");
        private static readonly int AnimDeath = Animator.StringToHash("Death");
        private static readonly int AnimDeathIndex = Animator.StringToHash("DeathIndex");
        private static readonly int AnimStateIdle = Animator.StringToHash("Idle");

        // Only the attack clip plays sped up; everything else runs at normal speed.
        private const float AttackPlaybackSpeed = 3f;

        private readonly Animator _animator;

        public EnemyAnimatorController(Animator animator)
        {
            _animator = animator;
        }

        public void ResetToRandomIdle()
        {
            if (_animator == null) return;

            // Also restores normal playback speed — covers the case where a restart
            // interrupts an enemy mid-attack (SelfDestructAfterAttackAnim's state guard
            // then skips TriggerDeath entirely), which would otherwise leave this enemy
            // permanently stuck at the 2x attack speed after being reused.
            _animator.speed = 1f;
            _animator.SetBool(AnimIsWalking, false);
            _animator.SetBool(AnimIsRunning, false);
            // Force back into Idle (a reused enemy's Animator may be frozen on a Death
            // state, which has no outgoing transition) at a random point in the looping
            // clip, so multiple enemies don't all animate in lockstep.
            _animator.Play(AnimStateIdle, 0, Random.value);
        }

        public void FreezeMovement()
        {
            if (_animator == null) return;

            if (_animator.GetBool(AnimIsWalking)) _animator.SetBool(AnimIsWalking, false);
            if (_animator.GetBool(AnimIsRunning)) _animator.SetBool(AnimIsRunning, false);
        }

        public void EnterWandering()
        {
            if (_animator == null) return;

            _animator.SetBool(AnimIsWalking, true);
            _animator.SetBool(AnimIsRunning, false);
        }

        public void StopWalking()
        {
            if (_animator == null) return;

            _animator.SetBool(AnimIsWalking, false);
        }

        public void EnterChasing()
        {
            if (_animator == null) return;

            _animator.SetBool(AnimIsWalking, false);
            _animator.SetBool(AnimIsRunning, true);
        }

        public void TriggerAttack()
        {
            if (_animator == null) return;

            _animator.SetBool(AnimIsWalking, false);
            _animator.SetBool(AnimIsRunning, false);
            _animator.speed = AttackPlaybackSpeed;
            _animator.SetTrigger(AnimAttack);
        }

        public void TriggerDeath(int deathIndex)
        {
            if (_animator == null) return;

            _animator.speed = 1f; // restore normal speed for the death sequence
            _animator.SetBool(AnimIsWalking, false);
            _animator.SetBool(AnimIsRunning, false);
            _animator.SetInteger(AnimDeathIndex, deathIndex);
            _animator.SetTrigger(AnimDeath);
        }
    }
}
