using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int hp = 100;
    public void ApplyDamage(int d)
    {
        hp -= d;
        if (hp <= 0) Die();
    }
    void Die() { /* whatever you do on death */ }
}