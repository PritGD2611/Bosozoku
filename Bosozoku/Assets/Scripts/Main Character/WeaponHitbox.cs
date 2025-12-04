using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WeaponHitbox : MonoBehaviour
{
    [Header("Damage Settings")]
    public int damage = 10;
    public LayerMask targetLayers = ~0; // layers allowed to be damaged
    public bool singleHitPerActivation = true;

    [Header("Owner")]
    public GameObject owner; // set to the character holding this weapon

    private Collider _col;
    private bool _active = false;
    private readonly HashSet<Health> _hitThisActivation = new HashSet<Health>();

    void Awake()
    {
        _col = GetComponent<Collider>();
        if (_col != null)
        {
            _col.isTrigger = true;
            _col.enabled = false; // default off; Activate controls it
        }
        if (owner == null)
        {
            // try to infer owner from parent
            owner = transform.root.gameObject;
        }
    }

    // Enable hit detection; optionally enable collider
    public void Activate()
    {
        _active = true;
        _hitThisActivation.Clear();
        if (_col != null) _col.enabled = true;
        // Debug
        Debug.Log($"[WeaponHitbox] Activated on {name} (owner: {(owner ? owner.name : "Unknown")})");
    }

    // Disable hit detection and clear activation hits; optionally disable collider
    public void Deactivate()
    {
        _active = false;
        _hitThisActivation.Clear();
        if (_col != null) _col.enabled = false;
        // Debug
        Debug.Log($"[WeaponHitbox] Deactivated on {name}");
    }

    // Clear record so same targets can be hit in a new activation
    public void ResetHits()
    {
        _hitThisActivation.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_active) return;
        if (other == null) return;

        // Layer filter
        int otherLayer = other.gameObject.layer;
        if (((1 << otherLayer) & targetLayers.value) == 0)
        {
            return; // not a valid target layer
        }

        // Find Health in the other hierarchy
        var health = other.GetComponentInParent<Health>();
        if (health == null) return;

        // Avoid multi-hits in a single activation if requested
        if (singleHitPerActivation && _hitThisActivation.Contains(health))
        {
            return;
        }

        // Apply damage
        health.TakeDamage(damage, owner);
        _hitThisActivation.Add(health);
        Debug.Log($"[WeaponHitbox] {name} hit {other.transform.root.name} for {damage}");

        // Optional hook: notify an OnWeaponHit handler on the owner if present
        if (owner != null)
        {
            owner.SendMessage("OnWeaponHit", other, SendMessageOptions.DontRequireReceiver);
        }
    }
}
