using UnityEngine;

public class HitboxProxy : MonoBehaviour
{
    public PlayerCombat owner;
    private void OnTriggerEnter(Collider other) => owner?.OnWeaponHit(other);
}
