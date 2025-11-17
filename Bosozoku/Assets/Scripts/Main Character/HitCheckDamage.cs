using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HitCheckDamage : MonoBehaviour
{
    // Optional owner (PlayerCombat). If set, hits are forwarded to owner.OnWeaponHit(...)
    public PlayerCombat owner;

    // Backwards-compat damage field for systems that call SetDamage(int)
    private int damage = 0;

    void Reset()
    {
        // Ensure collider is trigger by default (only in editor)
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    // Backwards-compatible API (some code may call this)
    public void SetDamage(int d)
    {
        damage = d;
    }

    private void OnTriggerEnter(Collider other)
    {
        // If we have an owner, forward the hit to PlayerCombat (preferred workflow)
        if (owner != null)
        {
            owner.OnWeaponHit(other);
            return;
        }

        // Fallback behaviour (keeps compatibility with older systems that expect HitCheckDamage to apply damage directly)
        if (damage <= 0) return; // nothing to do

        Transform root = other.transform.root;

        // 1) Try EnemyHealth on hit or root
        var enemyHealth = other.GetComponentInChildren<EnemyHealth>();
        if (enemyHealth == null)
            enemyHealth = root.GetComponentInChildren<EnemyHealth>();

        if (enemyHealth != null)
        {
            enemyHealth.ApplyDamage(damage);
            Debug.Log($"HitCheck: applied {damage} to {root.name} via EnemyHealth.ApplyDamage");
            return;
        }

        // 2) Try EnemyAI on root and call TakeDamage (SendMessage for flexible method name)
        var enemyAI = root.GetComponentInChildren<EnemyAI>();
        if (enemyAI != null)
        {
            // Prefer direct call if available
            enemyAI.ApplyDamage(damage);
            Debug.Log($"HitCheck: applied {damage} to {root.name} via EnemyAI.ApplyDamage");
            return;
        }

        // 3) Last fallback: SendMessage to try different method names on the hit object/root
        other.transform.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
        root.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);

        Debug.Log($"HitCheck: fallback SendMessage TakeDamage({damage}) to {root.name}");
    }
}
