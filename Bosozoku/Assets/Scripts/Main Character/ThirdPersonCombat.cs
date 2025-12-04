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
        public bool lockDuringAttack = false;

        [Tooltip("Randomize attacks or cycle them")]
        public bool randomizeAttacks = false;

        private int _lastLight = -1;
        private int _lastHeavy = -1;

        [SerializeField] private SphereCollider[] hitboxes;   // assign PlayerHitCheck here
        //[SerializeField] private PlayerWeaponHitbox[] hitboxDamagers; // assign same objects
        [SerializeField] private int baseDamage = 25;
        public int currentDamage = 25;




        private void Awake()
        {
            _anim = GetComponent<Animator>();
            _input = GetComponent<StarterAssetsInputs>();

            _anim = GetComponent<Animator>();
            _input = GetComponent<StarterAssetsInputs>();

            // safety: ensure hitboxes start disabled
            if (hitboxes != null)
                foreach (var h in hitboxes) if (h) h.enabled = false;

           /* if (hitboxDamagers != null)
                foreach (var d in hitboxDamagers) if (d) d.SetDamage(baseDamage);*/
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
            // in your Update() of the combat script on PlayerArmature


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
            // If blocking is active, attacks are disabled
            if (_anim.GetBool(IsBlockingHash)) return;

            // If lockDuringAttack is ON and we are currently in an attack, don't allow another
            bool busy = lockDuringAttack && _anim.GetBool(IsAttackingHash);

            if (!busy && _input.lightAttack)
            {
                // Pick one of the 4 light attack animations (Light0-Light3)
                int idx = PickIndex(4, ref _lastLight);
                _anim.SetInteger(LightIndexHash, idx);

                // Ensure heavy attack is not accidentally queued
                _anim.ResetTrigger(HeavyAttackHash);

                // Play light attack animation
                _anim.SetTrigger(LightAttackHash);
               // foreach (var d in hitboxDamagers) if (d) d.SetDamage(10);
            }
            else if (!busy && _input.heavyAttack)
            {
                // Pick one of the 4 heavy attack animations (Heavy0-Heavy3)
                int idx = PickIndex(4, ref _lastHeavy);
                _anim.SetInteger(HeavyIndexHash, idx);

                // Prevent light from mixing in
                _anim.ResetTrigger(LightAttackHash);

                // Play heavy attack animation
                _anim.SetTrigger(HeavyAttackHash);
                //foreach (var d in hitboxDamagers) if (d) d.SetDamage(25);
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

        void OnAnimatorMove()
        {
            if (_anim.GetBool(IsAttackingHash))
            {
                Vector3 delta = _anim.deltaPosition;
                delta.y = 0;
                transform.position += delta;
            }
        }

        public void OnAttackStart() 
        { 
            _anim.SetBool(IsAttackingHash, true);
            ToggleHitboxes(true);
        }
        public void OnAttackEnd() 
        {
            ToggleHitboxes(false);
            _anim.SetBool(IsAttackingHash, false);
        }

        private void ToggleHitboxes(bool state)
        {
            if (hitboxes == null) return;
            foreach (var h in hitboxes) if (h) h.enabled = state;
        }

        public void OnWeaponHit(Collider other)
        {
           /* var enemy = other.GetComponentInParent<EnemyAI>();
            if (enemy != null)
            {
                enemy.TakeDamage(currentDamage, other.ClosestPoint(transform.position), Vector3.up);
            }*/
        }

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
