using System.Collections;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    public GameObject playerHitCheck; // assign the PlayerHitCheck empty (has SphereCollider)
    public PlayerWeaponHitbox weaponHitbox; // assign the PlayerWeaponHitbox component (usually on the same hitcheck object)

    [Header("Light attack timings")]
    public float lightWindup = 0.05f;
    public float lightActive = 0.15f;
    public float lightRecovery = 0.2f;

    [Header("Heavy attack timings")]
    public float heavyWindup = 0.18f;
    public float heavyActive = 0.25f;
    public float heavyRecovery = 0.35f;

    private SphereCollider hitCollider;
    private bool isAttacking = false;

    void Start()
    {
        if (playerHitCheck) hitCollider = playerHitCheck.GetComponent<SphereCollider>();
        if (hitCollider != null) hitCollider.enabled = false;
        if (weaponHitbox == null && playerHitCheck != null)
            weaponHitbox = playerHitCheck.GetComponent<PlayerWeaponHitbox>();
    }

    void Update()
    {
        if (isAttacking) return;

        if (Input.GetMouseButtonDown(0)) // left click = light
            StartCoroutine(DoAttack(false));
        if (Input.GetMouseButtonDown(1)) // right click = heavy
            StartCoroutine(DoAttack(true));
    }

    private IEnumerator DoAttack(bool heavy)
    {
        isAttacking = true;

        if (heavy)
        {
            if (weaponHitbox != null) weaponHitbox.SetDamage(20);
            // windup
            yield return new WaitForSeconds(heavyWindup);
            // active window
            if (hitCollider != null) hitCollider.enabled = true;
            yield return new WaitForSeconds(heavyActive);
            if (hitCollider != null) hitCollider.enabled = false;
            // recovery
            yield return new WaitForSeconds(heavyRecovery);
        }
        else
        {
            if (weaponHitbox != null) weaponHitbox.SetDamage(10);
            yield return new WaitForSeconds(lightWindup);
            if (hitCollider != null) hitCollider.enabled = true;
            yield return new WaitForSeconds(lightActive);
            if (hitCollider != null) hitCollider.enabled = false;
            yield return new WaitForSeconds(lightRecovery);
        }

        isAttacking = false;
    }

    // If you prefer proxy pattern, HitboxProxy calls this
    public void OnWeaponHit(Collider other)
    {
        var enemy = other.GetComponentInParent<EnemyAI>();
        if (enemy != null)
        {
            // PlayerWeaponHitbox already triggers enemy.TakeDamage in its OnTriggerEnter,
            // so this method can be used for extra effects (particles, camera shake) if you want.
        }
    }
}
