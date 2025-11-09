using UnityEngine;

namespace StarterAssets
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(StarterAssetsInputs))]
    public class ThirdPersonCombat : MonoBehaviour
    {
        private Animator _anim;
        private StarterAssetsInputs _input;

        // Animator hashes
        private static readonly int LightAttackHash = Animator.StringToHash("LightAttack");
        private static readonly int HeavyAttackHash = Animator.StringToHash("HeavyAttack");
        private static readonly int BlockHash = Animator.StringToHash("Block");
        private static readonly int HitHash = Animator.StringToHash("Hit");

        private static readonly int LightIndexHash = Animator.StringToHash("LightIndex");
        private static readonly int HeavyIndexHash = Animator.StringToHash("HeavyIndex");
        private static readonly int HitIndexHash = Animator.StringToHash("HitIndex");

        private static readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");
        private static readonly int IsBlockingHash = Animator.StringToHash("IsBlocking");

        [Header("Config")]
        [Tooltip("Prevent new attacks until current finishes")]
        public bool lockDuringAttack = true;

        [Tooltip("Randomize attacks or cycle them")]
        public bool randomizeAttacks = true;

        private int _lastLight = -1;
        private int _lastHeavy = -1;

        private void Awake()
        {
            _anim = GetComponent<Animator>();
            _input = GetComponent<StarterAssetsInputs>();
        }

        private void Update()
        {
            HandleBlock();
            HandleAttacks();
            // reset one-frame inputs if you set them edge-based; here we read isPressed directly
            _input.lightAttack = false;
            _input.heavyAttack = false;
            _input.blockPressed = false;
            _input.blockReleased = false;
        }

        private void HandleBlock()
        {
            // Press to start block
            if (_input.blockPressed && !_anim.GetBool(IsAttackingHash))
            {
                _anim.SetBool(IsBlockingHash, true);
                _anim.ResetTrigger(LightAttackHash);
                _anim.ResetTrigger(HeavyAttackHash);
                _anim.SetTrigger(BlockHash);
            }

            // Release to stop block (exit transition goes back to locomotion)
            if (_input.blockReleased)
            {
                _anim.SetBool(IsBlockingHash, false);
            }
        }

        private void HandleAttacks()
        {
            if (_anim.GetBool(IsBlockingHash)) return;

            bool busy = lockDuringAttack && _anim.GetBool(IsAttackingHash);

            if (!busy && _input.lightAttack)
            {
                int idx = PickIndex(4, ref _lastLight);
                _anim.SetInteger(LightIndexHash, idx);
                _anim.ResetTrigger(HeavyAttackHash);
                _anim.SetTrigger(LightAttackHash);
                _anim.SetBool(IsAttackingHash, true);
            }
            else if (!busy && _input.heavyAttack)
            {
                int idx = PickIndex(4, ref _lastHeavy);
                _anim.SetInteger(HeavyIndexHash, idx);
                _anim.ResetTrigger(LightAttackHash);
                _anim.SetTrigger(HeavyAttackHash);
                _anim.SetBool(IsAttackingHash, true);
            }
        }

        private int PickIndex(int count, ref int last)
        {
            if (randomizeAttacks)
            {
                int r = Random.Range(0, count);
                if (count > 1 && r == last) r = (r + 1) % count; // avoid repeat
                last = r;
                return r;
            }
            else
            {
                last = (last + 1 + count) % count;
                return last;
            }
        }

        // === Called by Animation Events ===
        // Add these events in each attack clip:
        //   OnAttackStart() near the first frame that should lock movement / enable hitbox
        //   OnAttackEnd()   on the last frame to unlock
        public void OnAttackStart() { _anim.SetBool(IsAttackingHash, true); }
        public void OnAttackEnd() { _anim.SetBool(IsAttackingHash, false); }

        // Damage trigger (call from enemy or test)
        public void PlayHitReact(int hitIndex = -1)
        {
            if (_anim.GetBool(IsBlockingHash)) return; // blocked
            if (hitIndex < 0) hitIndex = Random.Range(0, 2); // 0..1
            _anim.SetInteger(HitIndexHash, hitIndex);
            _anim.SetTrigger(HitHash);
            _anim.SetBool(IsAttackingHash, false);
        }
    }
}
