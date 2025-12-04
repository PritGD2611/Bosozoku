using UnityEngine;
using UnityEngine.AI;

// Compact melee Enemy AI with a script-driven FSM (no Animator transitions required)
// States: Idle -> Chase -> Attack -> Hurt -> Dead
// Uses safe CrossFade calls and robust attack gating with animation-event hooks and a fallback timeout.
public class EnemyAi : MonoBehaviour
{
    [Header("References")]
    public Transform playerTransform;                 // auto-found in Awake if null
    public NavMeshAgent agent;                        // optional: if null, transform-based movement
    public Animator animator;                         // required for animations
    public WeaponHitbox weaponHitbox;                 // weapon trigger script

    [Header("Tuning")]
    public float chaseDistance = 12f;
    public float attackDistance = 2f;
    public float moveSpeed = 3f;                      // legacy fallback
    public float walkSpeed = 2.0f;                    // new: walk speed
    public float runSpeed = 4.5f;                     // new: run speed
    public float attackCooldown = 1.2f;
    public float maxAttackDuration = 2f;             // fallback if OnAttackEnd not called
    public int attackDamage = 15;
    public bool useRootMotion = false;                // if true, rely on animator root motion
    public float runThreshold = 6f;                   // if distance to player > this, use run anim/speed

    [Header("Animator States (exact names)")]
    public string idleState = "Idle";
    public string walkState = "Walk";                // walk locomotion
    public string runState = "Run";                  // run locomotion
    public string attackState = "Attack";            // ideally not looped
    public string hurtState = "Hurt";
    public string deathState = "Die";

    [Header("Debug")]
    public bool debug = true;

    private enum State { Idle, Chase, Attack, Hurt, Dead }
    private State currentState = State.Idle;

    private Health health;
    private string currentAnim = string.Empty;        // cache last played animation name

    private bool isAttacking = false;
    private float attackStartTime = -999f;
    private float nextAttackTime = 0f;               // cooldown gate set on attack end

    // Optional init for spawned enemies
    public void Init(Transform player)
    {
        playerTransform = player;
    }

    void Awake()
    {
        if (playerTransform == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p) playerTransform = p.transform;
            if (playerTransform == null && debug)
                Debug.LogWarning("[EnemyAI] Player not found by tag. Assign playerTransform in inspector.", this);
        }

        if (animator == null) animator = GetComponent<Animator>();
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        health = GetComponent<Health>();

        if (weaponHitbox != null)
        {
            weaponHitbox.damage = attackDamage;
            if (weaponHitbox.owner == null) weaponHitbox.owner = gameObject;
            weaponHitbox.Deactivate(); // ensure off by default
        }

        if (animator != null) animator.applyRootMotion = useRootMotion;

        ChangeState(State.Idle);
    }

    void OnEnable()
    {
        // Subscribe to health events
        if (health != null)
        {
            health.onHurt.AddListener(OnHurtEvent);
            health.onDead.AddListener(OnDeadEvent);
        }
    }

    void OnDisable()
    {
        if (health != null)
        {
            health.onHurt.RemoveListener(OnHurtEvent);
            health.onDead.RemoveListener(OnDeadEvent);
        }
    }

    void Update()
    {
        if (currentState == State.Dead) return;
        if (playerTransform == null)
        {
            // stay idle until player is known
            ChangeState(State.Idle);
            return;
        }

        float dist = Vector3.Distance(transform.position, playerTransform.position);

        // Always face the player in all active states
        FacePlayer(720f);

        // Attack fallback protection: if attack never ended (missing event or looped clip)
        if (isAttacking && (Time.time - attackStartTime) > maxAttackDuration)
        {
            if (debug) Debug.Log("[EnemyAI] Attack timeout. Auto-ending attack.", this);
            OnAttackEnd();
        }

        switch (currentState)
        {
            case State.Idle:
                // enter chase if within chase distance
                if (dist <= chaseDistance)
                {
                    ChangeState(State.Chase);
                }
                else
                {
                    PlayAnimSafe(idleState);
                    ApplyAgentSpeed(0f);
                }
                break;

            case State.Chase:
                // can start attack only if cooldown ready and within range
                if (!isAttacking && Time.time >= nextAttackTime && dist <= attackDistance)
                {
                    StartAttack();
                }
                else
                {
                    DoMovement(dist);
                    // Choose walk or run animation and speed based on distance
                    if (!string.IsNullOrEmpty(runState) && dist > runThreshold)
                    {
                        PlayAnimSafe(runState);
                        ApplyAgentSpeed(runSpeed);
                    }
                    else
                    {
                        PlayAnimSafe(walkState);
                        ApplyAgentSpeed(walkSpeed);
                    }

                    // fall back to idle if player is far
                    if (dist > chaseDistance * 1.2f)
                    {
                        ChangeState(State.Idle);
                    }
                }
                break;

            case State.Attack:
                // lock movement while attacking
                // wait for events; fallback handled above
                PlayAnimSafe(attackState);
                ApplyAgentSpeed(0f);
                break;

            case State.Hurt:
                // After hurt, decide next state quickly
                PlayAnimSafe(hurtState);
                if (!isAttacking && Time.time >= nextAttackTime && dist <= attackDistance)
                {
                    StartAttack();
                }
                else if (dist <= chaseDistance)
                {
                    ChangeState(State.Chase);
                }
                else
                {
                    ChangeState(State.Idle);
                }
                break;
        }
    }

    // Change state via single entry point
    private void ChangeState(State newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        if (debug) Debug.Log($"[EnemyAI] State -> {currentState}", this);

        switch (newState)
        {
            case State.Idle:
                PlayAnimSafe(idleState);
                if (agent != null) { agent.isStopped = false; agent.updateRotation = true; }
                break;

            case State.Chase:
                if (agent != null) { agent.isStopped = false; agent.updateRotation = true; }
                // animation and speed picked in Update based on distance (walk vs run)
                break;

            case State.Attack:
                isAttacking = true;
                attackStartTime = Time.time; // will be updated in OnAttackStart
                if (agent != null)
                {
                    agent.isStopped = true; // stop movement
                    agent.updateRotation = true; // allow agent rotation if needed
                }
                PlayAnimSafe(attackState);
                break;

            case State.Hurt:
                PlayAnimSafe(hurtState);
                break;

            case State.Dead:
                PlayAnimSafe(deathState);
                HandleDeath();
                break;
        }
    }

    // Safe animation trigger: avoids repeated CrossFade into same state each frame
    private void PlayAnimSafe(string stateName)
    {
        if (animator == null || string.IsNullOrEmpty(stateName)) return;
        if (currentAnim == stateName) return;
        animator.CrossFade(stateName, 0.08f, 0, 0f);
        currentAnim = stateName;
    }

    private void DoMovement(float dist)
    {
        if (useRootMotion) return; // let Animator drive via root motion
        if (playerTransform == null) return;

        float desiredSpeed = (dist > runThreshold) ? runSpeed : walkSpeed;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.speed = desiredSpeed;
            agent.stoppingDistance = Mathf.Max(0.05f, attackDistance * 0.6f);
            agent.SetDestination(playerTransform.position);
        }
        else
        {
            Vector3 targetPos = playerTransform.position;
            Vector3 dir = targetPos - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
            {
                Vector3 step = Vector3.MoveTowards(transform.position, targetPos, desiredSpeed * Time.deltaTime);
                transform.position = step;
                // face handled globally in Update (FacePlayer)
            }
        }
    }

    private void ApplyAgentSpeed(float s)
    {
        if (agent != null)
        {
            agent.speed = s > 0f ? s : 0f;
        }
    }

    private void FacePlayer(float turnSpeedDegPerSec)
    {
        if (playerTransform == null) return;
        Vector3 dir = playerTransform.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        Quaternion look = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeedDegPerSec * Time.deltaTime);
    }

    // Attack gating: called when conditions satisfied
    private void StartAttack()
    {
        if (isAttacking) return;
        if (Time.time < nextAttackTime) return;
        ChangeState(State.Attack);
        if (debug) Debug.Log("[EnemyAI] StartAttack()", this);
    }

    // Animation event: called at the attack hit start frame
    public void OnAttackStart()
    {
        isAttacking = true;
        attackStartTime = Time.time;
        if (weaponHitbox != null)
        {
            weaponHitbox.damage = attackDamage;
            weaponHitbox.Activate();
        }
        if (debug) Debug.Log("[EnemyAI] OnAttackStart", this);
    }

    // Animation event: called at the attack hit end frame
    public void OnAttackEnd()
    {
        if (weaponHitbox != null)
        {
            weaponHitbox.Deactivate();
        }
        isAttacking = false;
        nextAttackTime = Time.time + attackCooldown; // start cooldown now
        // return to chase if still alive
        if (currentState != State.Dead)
        {
            ChangeState(State.Chase);
        }
        if (debug) Debug.Log("[EnemyAI] OnAttackEnd", this);
    }

    private void OnHurtEvent()
    {
        if (currentState == State.Dead) return;
        ChangeState(State.Hurt);
    }

    private void OnDeadEvent()
    {
        if (currentState == State.Dead) return;
        ChangeState(State.Dead);
    }

    private void HandleDeath()
    {
        // Deactivate weapon
        if (weaponHitbox != null) weaponHitbox.Deactivate();

        // Stop movement
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.enabled = false;
        }

        // Disable colliders
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;

        // Disable this AI component
        enabled = false;
    }
}
