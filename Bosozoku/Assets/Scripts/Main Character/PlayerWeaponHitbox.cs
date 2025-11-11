using UnityEngine;

public class PlayerWeaponHitbox : MonoBehaviour
{
    public int damage = 25;
    

    private void OnTriggerEnter(Collider other)
    {
        var enemy = other.GetComponentInParent<EnemyAI>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage, other.ClosestPoint(transform.position), Vector3.up);
        }
    }

    public void SetDamage(int value)
    {
        damage = value;
    }
}
