using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    public int lightDamage = 10;
    public int heavyDamage = 20;
    public Animator anim;

    private bool isHeavy = false;

    // use triggers named exactly like these in the Animator controller
    private static readonly int TrigLight = Animator.StringToHash("LightAttack");
    private static readonly int TrigHeavy = Animator.StringToHash("HeavyAttack");

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // left
        {
            isHeavy = false;
            if (anim == null) { Debug.LogWarning("Animator not assigned on PlayerCombat"); return; }
            anim.SetTrigger(TrigLight);         // safer than CrossFade(string,...)
        }

        if (Input.GetMouseButtonDown(1)) // right
        {
            isHeavy = true;
            if (anim == null) { Debug.LogWarning("Animator not assigned on PlayerCombat"); return; }
            anim.SetTrigger(TrigHeavy);
        }
    }

    // called by HitCheckDamage
    public void OnWeaponHit(Collider other)
    {
        //var enemy = other.GetComponentInParent<EnemyAI>();
        /*if (enemy)
        {
            int dmg = isHeavy ? heavyDamage : lightDamage;
            enemy.ApplyDamage(dmg);
        }*/
    }
}
