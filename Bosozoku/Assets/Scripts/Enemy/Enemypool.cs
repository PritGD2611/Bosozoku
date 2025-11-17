using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PooledEnemy : MonoBehaviour
{
    private EnemySpawner spawner;
    private GameObject prefabOrigin; // which prefab queue to return to

    public void SetSpawner(EnemySpawner s) => spawner = s;
    public void SetPrefabOrigin(GameObject prefab) => prefabOrigin = prefab;

    // Call this from your enemy logic when the enemy dies
    public void Die()
    {
        // run death VFX/anim here if you want
        // Then notify spawner and return to pool
        spawner?.NotifyEnemyDeath(this);

        if (spawner != null && prefabOrigin != null)
        {
            spawner.ReturnToPool(gameObject, prefabOrigin);
        }
        else
        {
            // fallback: deactivate
            gameObject.SetActive(false);
        }
    }

    // Example: auto-return if disabled externally (keeps counts consistent)
    private void OnDisable()
    {
        // nothing else here to avoid double-returning; the spawner logic handles the queueing
    }
}
