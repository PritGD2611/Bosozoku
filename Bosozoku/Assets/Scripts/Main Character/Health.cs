using UnityEngine;
using UnityEngine.Events;
using UnityEngine.AI;

public class Health : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Events")]
    public UnityEvent onHurt;
    public UnityEvent onDead;

    [Header("Damage Cooldown")]
    public float damageCooldown = 0.1f;

    [Header("Optional UI Hooks")]
    public GameManager gameManager;            // optional: will be called on health changes
    public MonoBehaviour UIHealthController;   // optional script with method UpdateHealth(float normalized)

    private float _lastDamageTime = -999f;
    private bool _isDead = false;

    void Awake()
    {
        currentHealth = maxHealth;
        _isDead = false;
    }

    // Returns true if currentHealth > 0
    public bool IsAlive()
    {
        return !_isDead && currentHealth > 0;
    }

    // Normalized health value 0..1 for UI
    public float GetHealthNormalized()
    {
        return maxHealth > 0 ? Mathf.Clamp01((float)currentHealth / (float)maxHealth) : 0f;
    }

    // Apply healing; clamps to maxHealth
    public void Heal(int amount)
    {
        if (_isDead) return;
        if (amount <= 0) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        NotifyUI();
    }

    // Apply damage with cooldown; source is the GameObject that caused damage (can be null)
    public void TakeDamage(int amount, GameObject source)
    {
        if (_isDead) return;
        if (Time.time - _lastDamageTime < damageCooldown) return;
        _lastDamageTime = Time.time;

        if (amount <= 0) return;

        int before = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - amount);
        Debug.Log($"[Health] {name} took {amount} damage from {(source ? source.name : "Unknown")} (HP {before} -> {currentHealth})");

        onHurt?.Invoke();
        NotifyUI();

        if (currentHealth <= 0 && !_isDead)
        {
            _isDead = true;
            Debug.Log($"[Health] {name} died.");
            onDead?.Invoke();
            HandleDeathDisable();
            NotifyUI();
        }
    }

    // Disable common components/behaviours on death
    private void HandleDeathDisable()
    {
        // Disable colliders
        foreach (var col in GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }

        // Stop NavMeshAgent if present
        var agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.enabled = false;
        }

        // Disable Animator if present
        var anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.applyRootMotion = false;
            anim.enabled = false;
        }

        // Disable StarterAssets ThirdPersonController if present
        var tpc = GetComponent<StarterAssets.ThirdPersonController>();
        if (tpc != null) tpc.enabled = false;

        // Finally, disable this component itself
        enabled = false;
    }

    // Notify GameManager/UI if assigned
    private void NotifyUI()
    {
        float norm = GetHealthNormalized();
        if (gameManager != null)
        {
            // If GameManager has a method to handle health updates, call it via SendMessage to avoid hard ref
            gameManager.SendMessage("OnPlayerHealthChanged", norm, SendMessageOptions.DontRequireReceiver);
        }
        if (UIHealthController != null)
        {
            UIHealthController.SendMessage("UpdateHealth", norm, SendMessageOptions.DontRequireReceiver);
        }
    }
}
