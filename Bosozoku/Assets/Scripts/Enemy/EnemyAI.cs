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

    public float minAttackCooldown = 2f;
    public float maxAttackCooldown = 3f;

    [Header("Root Motion Control")] 
    [Tooltip("Use root motion while in Walk animation")]
    public bool useRootMotionWalk = false; // OFF: let NavMeshAgent drive locomotion
    [Tooltip("Use root motion while in Run animation")]
    public bool useRootMotionRun = false;  // OFF: let NavMeshAgent drive locomotion
    [Tooltip("Use root motion while in Attack animation")]
    public bool useRootMotionAttack = true; // keep RM for attack lunge/steps

    private static readonly int HashIdle = Animator.StringToHash("Idle");
    private static readonly int HashWalk = Animator.StringToHash("Walk");
    private static readonly int HashRun = Animator.StringToHash("Run");
    private static readonly int HashAttack = Animator.StringToHash("Attack");

    private float lastAttackTime = -999f;
    private bool isDead = false;
    private bool isAttacking = false;

    private bool allowRootMotion = false;

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

        if (agent != null)
            agent.stoppingDistance = Mathf.Max(agent.stoppingDistance, attackRange);

        if (anim != null)
            anim.applyRootMotion = false; // default off; we enable only during attack

        if (agent != null)
        {
            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.autoBraking = true;
        }

        isDead = false;
}

    void Update()
    {
        if (isDead || player == null) { IdleAnim(); UpdateRootMotionMode(); return; }

        if (isAttacking)
        {
            FaceTarget(20f);
            UpdateRootMotionMode();
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

        UpdateRootMotionMode();
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
            agent.updatePosition = true; // ensure agent moves us
            agent.updateRotation = true; // ensure agent rotates us
            agent.SetDestination(player.position);
        }

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

        FaceTarget(20f);
        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        attackCooldown = Random.Range(minAttackCooldown, maxAttackCooldown);

        agent.isStopped = true;
        agent.updatePosition = !useRootMotionAttack;
        agent.updateRotation = !useRootMotionAttack;

        anim.CrossFade(HashAttack, 0.05f, 0, 0f);

        yield return null;
        yield return new WaitForSeconds(windupTime);

        if (hitTrigger) hitTrigger.enabled = true;
        yield return new WaitForSeconds(hitActiveTime);
        if (hitTrigger) hitTrigger.enabled = false;

        yield return new WaitUntil(() =>
        {
            var s = anim.GetCurrentAnimatorStateInfo(0);
            return s.shortNameHash == HashAttack && s.normalizedTime >= 0.98f && !anim.IsInTransition(0);
        });

        yield return new WaitForSeconds(0.1f);

        isAttacking = false;
        agent.isStopped = false;
        agent.updatePosition = true;  // return control to agent for locomotion
        agent.updateRotation = true;
    }

    private void FaceTarget(float turnSpeed)
    {
        Vector3 dir = (player.position - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        Quaternion look = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeed * Time.deltaTime * 100f);
    }

    private void IdleAnim() => CrossFadeIfNeeded(HashIdle, 0.1f);
    private void WalkAnim() => CrossFadeIfNeeded(HashWalk, 0.08f);
    private void RunAnim() => CrossFadeIfNeeded(HashRun, 0.08f);

    private void CrossFadeIfNeeded(int stateHash, float fade)
    {
        if (anim.IsInTransition(0)) return;
        var info = anim.GetCurrentAnimatorStateInfo(0);
        if (info.shortNameHash == stateHash) return;
        anim.CrossFade(stateHash, fade, 0, 0f);
    }

    private void UpdateRootMotionMode()
    {
        if (anim == null) return;

        var info = anim.GetCurrentAnimatorStateInfo(0);
        int current = info.shortNameHash;

        bool inAttack = current == HashAttack || isAttacking;

        bool shouldUseRM = useRootMotionAttack && inAttack;

        allowRootMotion = shouldUseRM;
        anim.applyRootMotion = shouldUseRM;

        // For non-attack, keep animator speed at 1 so foot timing is consistent while agent moves us
        if (!shouldUseRM) anim.speed = 1f;

        if (agent)
        {
            agent.updatePosition = !shouldUseRM ? true : false; // agent drives position unless attacking with RM
            agent.updateRotation = !shouldUseRM ? true : false;
            if (shouldUseRM)
            {
                agent.nextPosition = transform.position;
            }
        }
    }

    private void OnAnimatorMove()
    {
        if (!allowRootMotion || anim == null) return;

        Vector3 delta = anim.deltaPosition;
        delta.y = 0f;
        transform.position += delta;
        transform.rotation *= anim.deltaRotation;

        if (agent)
        {
            agent.nextPosition = transform.position;
        }
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
    }

    private void Die()
    {
        isDead = true;
        agent.isStopped = true;
        agent.ResetPath();
        IdleAnim();
        DisableAllCollisions();
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
