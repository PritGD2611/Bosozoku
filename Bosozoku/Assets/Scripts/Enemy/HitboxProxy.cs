using UnityEngine;

public class HitboxProxy : MonoBehaviour
{
    public StarterAssets.ThirdPersonCombat owner;
    private void OnTriggerEnter(Collider other) => owner?.OnWeaponHit(other);
}


