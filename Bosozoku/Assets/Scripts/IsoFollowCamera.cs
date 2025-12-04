using UnityEngine;

[DefaultExecutionOrder(1000)]
public class IsoFollowCamera : MonoBehaviour
{
    public Transform target;
    public float targetHeight = 1.6f;

    public float isoPitch = 35f;
    public float isoYaw = 45f;
    public float distance = 8f;
    public float minDistance = 4f;
    public float maxDistance = 12f;

    public float positionDamping = 12f;
    public float rotationDamping = 12f;

    public bool useCollision = true;
    public LayerMask collisionLayers = ~0;
    public float collisionRadius = 0.25f;
    public float collisionSkin = 0.1f;

    public bool allowZoom = true;
    public bool allowSnapRotate = true;
    public KeyCode rotateLeftKey = KeyCode.Q;
    public KeyCode rotateRightKey = KeyCode.E;

    private Vector3 _vel;
    private float _targetYaw;

    void Awake()
    {
        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player) target = player.transform;
        }
        _targetYaw = isoYaw;
    }

    void Update()
    {
        if (allowZoom)
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.001f)
            {
                distance = Mathf.Clamp(distance - scroll, minDistance, maxDistance);
            }
        }

        if (allowSnapRotate)
        {
            if (Input.GetKeyDown(rotateLeftKey)) _targetYaw -= 90f;
            if (Input.GetKeyDown(rotateRightKey)) _targetYaw += 90f;
        }

        isoYaw = Mathf.LerpAngle(isoYaw, _targetYaw, 1f - Mathf.Exp(-rotationDamping * Time.deltaTime));
    }

    void LateUpdate()
    {
        if (!target) return;

        Vector3 lookPoint = target.position + Vector3.up * targetHeight;
        Quaternion isoRot = Quaternion.Euler(isoPitch, isoYaw, 0f);
        Vector3 viewDir = isoRot * Vector3.forward;
        Vector3 desiredPos = lookPoint - viewDir * distance;

        if (useCollision)
        {
            Vector3 from = lookPoint;
            Vector3 to = desiredPos;
            Vector3 delta = to - from;
            float castDist = delta.magnitude;

            if (castDist > 0.001f)
            {
                // Cast for all potential obstructions and choose the nearest valid one
                RaycastHit[] hits = Physics.SphereCastAll(from, collisionRadius, delta.normalized, castDist, collisionLayers, QueryTriggerInteraction.Ignore);
                float nearestValid = castDist;

                for (int i = 0; i < hits.Length; i++)
                {
                    var hit = hits[i];
                    var hcol = hit.collider;
                    if (hcol == null) continue;

                    // Ignore our target hierarchy
                    if (target != null && hcol.transform.IsChildOf(target)) continue;

                    // Ignore character colliders by tag to prevent zoom-in when bumping enemies or player
                    if (hcol.CompareTag("Player") || hcol.CompareTag("Enemy")) continue;

                    // Only consider solid geometry; triggers already filtered by QueryTriggerInteraction.Ignore
                    // Keep the nearest hit that is not ignored
                    if (hit.distance < nearestValid)
                    {
                        nearestValid = hit.distance;
                    }
                }

                if (nearestValid < castDist)
                {
                    float safeDist = Mathf.Max(0f, nearestValid - collisionSkin);
                    desiredPos = from + delta.normalized * safeDist;
                }
            }
        }

        Vector3 newPos = Vector3.SmoothDamp(transform.position, desiredPos, ref _vel,
            positionDamping > 0f ? (1f / positionDamping) : 0f);

        transform.position = newPos;

        Quaternion desiredRot = Quaternion.LookRotation((lookPoint - newPos).normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot,
            1f - Mathf.Exp(-rotationDamping * Time.deltaTime));
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!target) return;
        Vector3 lookPoint = target.position + Vector3.up * targetHeight;
        Quaternion isoRot = Quaternion.Euler(isoPitch, isoYaw, 0f);
        Vector3 viewDir = isoRot * Vector3.forward;
        Vector3 desiredPos = lookPoint - viewDir * Mathf.Clamp(distance, minDistance, maxDistance);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(lookPoint, desiredPos);
        Gizmos.DrawWireSphere(lookPoint, 0.1f);
        Gizmos.DrawWireSphere(desiredPos, collisionRadius);
    }
#endif
}
