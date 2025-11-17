using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Prefabs & Pooling")]
    [Tooltip("Enemy prefabs to spawn (add 1..N)")]
    public List<GameObject> enemyPrefabs = new List<GameObject>();
    [Tooltip("Total number of pooled instances per prefab (recommend >= waves * spawnPerWave / prefabs)")]
    public int poolSizePerPrefab = 6;

    [Header("Spawn/Wave Settings")]
    [Tooltip("How many enemies spawn at the same time (per wave)")]
    public int spawnPerWave = 3;
    [Tooltip("How many waves in total")]
    public int totalWaves = 3;
    [Tooltip("Delay between finishing a wave and starting next (seconds)")]
    public float delayBetweenWaves = 1f;

    [Header("Spawn Locations")]
    [Tooltip("If empty, spawns are random inside radius around the spawner.")]
    public Transform[] spawnPoints;
    [Tooltip("If spawnPoints is empty, spawn within this radius")]
    public float spawnRadius = 5f;
    [Tooltip("Max attempts to find non-overlapping point when randomizing")]
    public int maxSpawnAttempts = 10;

    // internal pools: one queue per prefab
    private Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();
    private int currentWave = 0;
    private int activeEnemies = 0;
    private bool running = false;

    void Start()
    {
        if (enemyPrefabs == null || enemyPrefabs.Count == 0)
        {
            Debug.LogWarning("EnemySpawner: No enemy prefabs assigned.");
            return;
        }

        InitializePools();
        StartCoroutine(RunWaves());
    }

    void InitializePools()
    {
        pools.Clear();

        foreach (var prefab in enemyPrefabs)
        {
            Queue<GameObject> q = new Queue<GameObject>();
            for (int i = 0; i < poolSizePerPrefab; i++)
            {
                GameObject inst = Instantiate(prefab, transform); // parent under spawner for neat hierarchy
                inst.SetActive(false);

                // Ensure pooled prefab has PooledEnemy component
                var pe = inst.GetComponent<PooledEnemy>();
                if (pe == null)
                {
                    pe = inst.AddComponent<PooledEnemy>();
                }
                pe.SetSpawner(this); // so that enemy can notify spawner on death

                q.Enqueue(inst);
            }
            pools[prefab] = q;
        }
    }

    IEnumerator RunWaves()
    {
        running = true;
        currentWave = 0;

        while (currentWave < totalWaves)
        {
            currentWave++;
            Debug.Log($"EnemySpawner: Starting wave {currentWave}/{totalWaves}");
            SpawnWave();
            // wait until all spawned enemies die
            yield return new WaitUntil(() => activeEnemies <= 0);
            Debug.Log($"EnemySpawner: Wave {currentWave} finished");

            if (currentWave < totalWaves)
                yield return new WaitForSeconds(delayBetweenWaves);
        }

        running = false;
        Debug.Log("EnemySpawner: All waves complete");
        yield break;
    }

    void SpawnWave()
    {
        for (int i = 0; i < spawnPerWave; i++)
        {
            SpawnOne();
        }
    }

    void SpawnOne()
    {
        // choose a random prefab from the list
        var prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
        var pooledObj = GetFromPool(prefab);
        if (pooledObj == null)
        {
            Debug.LogWarning("EnemySpawner: Pool exhausted and couldn't create more.");
            return;
        }

        // determine spawn position
        Vector3 pos = transform.position;
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            // pick a random spawn point
            var sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
            pos = sp.position;
        }
        else
        {
            // random point in radius
            Vector3 attemptPos = transform.position;
            for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
            {
                Vector2 circle = Random.insideUnitCircle * spawnRadius;
                attemptPos = transform.position + new Vector3(circle.x, 0f, circle.y);
                // Could add overlap checks here if needed
                break;
            }
            pos = attemptPos;
        }

        pooledObj.transform.position = pos;
        pooledObj.transform.rotation = Quaternion.identity;
        pooledObj.SetActive(true);

        // If it has a PooledEnemy, inform it which prefab type it belongs to (so it can return to correct pool)
        var pe = pooledObj.GetComponent<PooledEnemy>();
        if (pe != null) pe.SetPrefabOrigin(prefab);

        activeEnemies++;
    }

    GameObject GetFromPool(GameObject prefab)
    {
        if (!pools.ContainsKey(prefab))
        {
            Debug.LogError("EnemySpawner: Prefab not in pool dictionary. Did you add it to enemyPrefabs?");
            return null;
        }

        var q = pools[prefab];
        if (q.Count == 0)
        {
            // optionally instantiate more if pool exhausted (simple strategy)
            GameObject inst = Instantiate(prefab, transform);
            inst.SetActive(false);
            var pe = inst.GetComponent<PooledEnemy>();
            if (pe == null) pe = inst.AddComponent<PooledEnemy>();
            pe.SetSpawner(this);
            return inst;
        }
        else
        {
            return q.Dequeue();
        }
    }

    // Called by PooledEnemy when it dies/gets disabled and should return to pool
    public void ReturnToPool(GameObject pooledObj, GameObject prefabOrigin)
    {
        pooledObj.SetActive(false);
        // reset transforms/state if needed here

        // return to correct queue
        if (!pools.ContainsKey(prefabOrigin))
        {
            // If no pool (rare), destroy
            Destroy(pooledObj);
            return;
        }

        pools[prefabOrigin].Enqueue(pooledObj);
    }

    // Called by PooledEnemy to signal death
    public void NotifyEnemyDeath(PooledEnemy pe)
    {
        activeEnemies = Mathf.Max(0, activeEnemies - 1);
    }

    // Utility & debug
    private void OnDrawGizmosSelected()
    {
        if ((spawnPoints == null || spawnPoints.Length == 0) && spawnRadius > 0f)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
        }
        if (spawnPoints != null)
        {
            Gizmos.color = Color.green;
            foreach (var sp in spawnPoints)
                if (sp != null) Gizmos.DrawSphere(sp.position, 0.2f);
        }
    }

    // Public control (optional)
    public void Restart()
    {
        StopAllCoroutines();
        // deactivate all pooled items
        foreach (var kv in pools)
        {
            foreach (var obj in kv.Value)
                if (obj != null) obj.SetActive(false);
        }
        activeEnemies = 0;
        StartCoroutine(RunWaves());
    }

    public bool IsRunning => running;
    public int CurrentWave => currentWave;
    public int ActiveEnemies => activeEnemies;
}
