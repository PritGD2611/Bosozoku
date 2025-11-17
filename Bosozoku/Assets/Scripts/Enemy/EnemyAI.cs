using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    public Transform player;                 // drag your player here or leave empty to auto-find by tag "Player"
    public Transform hitCheck;               // the child on the crowbar tip/hand
    public SphereCollider hitTrigger;        // trigger collider on hitCheck (disabled by default)

    private NavMeshAgent agent;
    private Animator anim;

    [Header("Detection & Combat")]
    public float detectRadius = 15f;         // start chasing when player enters this
    public float attackRange = 1.5f;         // must be <= agent.stoppingDistance
    public float attackCooldown = 2.5f;       // seconds between attacks
    public int damagePerHit = 5;            // damage dealt to player per hit
    public float windupTime = 0.25f;         // time before enabling hit
    public float hitActiveTime = 0.20f;      // how long the crowbar trigger is active

    [Header("Movement Speeds")]
    public float walkSpeed = 2.0f;
    public float runSpeed = 3.8f;
    public float runChaseThreshold = 7.0f;   // if distance > this ? run, else walk

    [Header("Health")]
    public int maxHealth = 50;
    public int currentHealth;

    // add these for cooldown randomization if you prefer explicit bounds
    public float minAttackCooldown = 2f;
    public float maxAttackCooldown = 3f;

    // Animator state names (must match states in controller)
    private static readonly int HashIdle = Animator.StringToHash("Idle");
    private static readonly int HashWalk = Animator.StringToHash("Walk");
    private static readonly int HashRun = Animator.StringToHash("Run");
    private static readonly int HashAttack = Animator.StringToHash("Attack");
    // Optional: add Hurt/Die if you have them
    // private static readonly int HashHurt = Animator.StringToHash("Hurt");
    // private static readonly int HashDie  = Animator.StringToHash("Die");

    private float lastAttackTime = -999f;
    private bool isDead = false;
    private bool isAttacking = false;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        

        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        currentHealth = maxHealth;

        if (hitTrigger != null)
            hitTrigger.enabled = false;

        // Ensure stopping distance matches attack range
        if (agent != null)
            agent.stoppingDistance = Mathf.Max(agent.stoppingDistance, attackRange);
        isDead = false;
}

    void Update()
    {
        if (isDead || player == null) { IdleAnim(); return; }

        if (isAttacking) // <- early out, nothing else can stomp Attack
        {
            FaceTarget(20f);
            return;
        }

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist < 4f) FaceTarget(10f);

        if (dist <= attackRange)
        {
            agent.ResetPath();
            TryAttack();
        }
        else if (dist <= detectRadius)
        {
            Chase(dist);
        }
        else
        {
            PatrolIdle();
        }

        // Safety: keep agent base offset sane (prevents floating/foot sinking if model scaled)
        if (agent && agent.isOnNavMesh && !agent.isStopped && agent.desiredVelocity.sqrMagnitude > 0.01f)
        {
            // nothing special needed here, but you can clamp base offset if your rig floats
        }
    }

    private void PatrolIdle()
    {
        if (isAttacking) return;
        agent.isStopped = true;
        agent.ResetPath();
        IdleAnim();
    }

    private void Chase(float distanceToPlayer)
    {
        if (isAttacking) return;

        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }

        // Choose walk vs run
        if (distanceToPlayer > runChaseThreshold)
        {
            agent.speed = runSpeed;
            RunAnim();
        }
        else
        {
            agent.speed = walkSpeed;
            WalkAnim();
        }
    }

    private void TryAttack()
    {
        if (isAttacking) return;
        if (Time.time - lastAttackTime < attackCooldown) { IdleAnim(); return; }

        // In range: face player and perform attack
        FaceTarget(20f);
        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        attackCooldown = Random.Range(minAttackCooldown, maxAttackCooldown);

        agent.isStopped = true;

        // fire the attack
        anim.CrossFade(HashAttack, 0.05f, 0, 0f);

        // optional: 1 frame to enter the state
        yield return null;

        // WINDUP (use clip time if you want, but keep your constants if they feel good)
        yield return new WaitForSeconds(windupTime);

        if (hitTrigger) hitTrigger.enabled = true;

        yield return new WaitForSeconds(hitActiveTime);

        if (hitTrigger) hitTrigger.enabled = false;

        // Now wait for the clip to actually finish (? normalizedTime >= 0.98)
        // This prevents “half-played” feel if anything tries to blend out too soon.
        yield return new WaitUntil(() =>
        {
            var s = anim.GetCurrentAnimatorStateInfo(0);
            return s.shortNameHash == HashAttack && s.normalizedTime >= 0.98f && !anim.IsInTransition(0);
        });

        // small recovery so we don’t snap
        yield return new WaitForSeconds(0.1f);

        isAttacking = false;
        agent.isStopped = false;
    }

    private void FaceTarget(float turnSpeed)
    {
        Vector3 dir = (player.position - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        Quaternion look = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeed * Time.deltaTime * 100f);
    }

    // --- Animation helpers (no transitions in the controller needed) ---
    private void IdleAnim() => CrossFadeIfNeeded(HashIdle, 0.1f);
    private void WalkAnim() => CrossFadeIfNeeded(HashWalk, 0.08f);
    private void RunAnim() => CrossFadeIfNeeded(HashRun, 0.08f);

    private void CrossFadeIfNeeded(int stateHash, float fade)
    {
        // do NOT stomp while transitioning
        if (anim.IsInTransition(0)) return;

        var info = anim.GetCurrentAnimatorStateInfo(0);
        if (info.shortNameHash == stateHash) return;

        // start from normalizedTime 0 to avoid “half-play”
        anim.CrossFade(stateHash, fade, 0, 0f);
    }

    // --- Damage system ---
    // Existing detailed TakeDamage kept for hit-point/normal aware callers:
    public void TakeDamage(int amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (isDead) return;
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        // Optional: flinch
        // anim.CrossFade(HashHurt, 0.05f);
    }

    private void Die()
    {
        isDead = true;
        agent.isStopped = true;
        agent.ResetPath();
        // Optional: anim.CrossFade(HashDie, 0.1f);
        // If you don’t have a die clip, just idle/freeze and disable colliders:
        IdleAnim();
        DisableAllCollisions();
        // Destroy after a delay if you want
        Destroy(gameObject, 5f);
    }

    private void DisableAllCollisions()
    {
        foreach (var c in GetComponentsInChildren<Collider>())
            c.enabled = false;
    }

    // Attack trigger ? apply damage when overlapping the player
    private void OnTriggerEnter(Collider other)
    {
        // Only consider our attack trigger
        if (hitTrigger == null || other == null) return;
        if (!hitTrigger.enabled) return; // only during strike window

        if (other.CompareTag("Player"))
        {
            var hp = other.GetComponentInParent<PlayerHealth>();
            if (hp) hp.TakeDamage(damagePerHit);
        }
    }

    // Convenience overloads / entry points so external scripts can damage this enemy easily:
    // Call any of these from player/hit scripts:
    //   enemy.GetComponent<EnemyAI>().ApplyDamage(10);
    //   enemy.SendMessage("ApplyDamage", 10); // SendMessage will also work
    public void ApplyDamage(int amount)
    {
        // simple call with no hit point info
        TakeDamage(amount, transform.position, Vector3.up);
    }

    public void TakeDamage(int amount)
    {
        // overloaded name for convenience
        TakeDamage(amount, transform.position, Vector3.up);
    }

    public void ReceiveDamage(int amount)
    {
        // another common name some scripts use
        TakeDamage(amount, transform.position, Vector3.up);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
