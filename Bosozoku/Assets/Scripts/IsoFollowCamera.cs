using UnityEngine;

[DefaultExecutionOrder(1000)]
public class IsoFollowCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    [Tooltip("Where on the target to look (in local up). ~1.6 for human head level.")]
    public float targetHeight = 1.6f;

    [Header("Isometric View")]
    [Range(0f, 89f)] public float isoPitch = 35f;   // tilt down
    [Range(0f, 360f)] public float isoYaw = 45f;    // rotate around Y (45° = classic iso)
    public float distance = 8f;
    public float minDistance = 4f;
    public float maxDistance = 12f;

    [Header("Smoothing")]
    [Tooltip("Higher = snappier. 0 = instant.")]
    public float positionDamping = 12f;
    public float rotationDamping = 12f;

    [Header("Collision (optional)")]
    public bool useCollision = true;
    public LayerMask collisionLayers = ~0;   // everything by default
    [Tooltip("Radius for cast; small but non-zero prevents clipping.")]
    public float collisionRadius = 0.25f;
    public float collisionSkin = 0.1f;

    [Header("Controls (optional)")]
    public bool allowZoom = true;            // Mouse wheel zoom
    public bool allowSnapRotate = true;      // Q/E snap 90°
    public KeyCode rotateLeftKey = KeyCode.Q;
    public KeyCode rotateRightKey = KeyCode.E;

    private Vector3 _vel; // for SmoothDamp
    private float _targetYaw;

    void Awake()
    {
        if (target == null)
        {
            // Try to auto-find the player by tag
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player) target = player.transform;
        }
        _targetYaw = isoYaw;
    }

    void Update()
    {
        // Optional inputs kept in Update
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

        // Smooth the yaw toward target yaw so snaps feel nice
        isoYaw = Mathf.LerpAngle(isoYaw, _targetYaw, 1f - Mathf.Exp(-rotationDamping * Time.deltaTime));
    }

    void LateUpdate()
    {
        if (!target) return;

        // Where we want to look
        Vector3 lookPoint = target.position + Vector3.up * targetHeight;

        // Compute camera orientation from iso angles
        Quaternion isoRot = Quaternion.Euler(isoPitch, isoYaw, 0f);
        Vector3 viewDir = isoRot * Vector3.forward;        // direction the camera looks
        Vector3 desiredPos = lookPoint - viewDir * distance;

        // Handle collision: push camera closer if blocked
        if (useCollision)
        {
            Vector3 from = lookPoint;
            Vector3 to = desiredPos;
            Vector3 delta = to - from;
            float castDist = delta.magnitude;

            if (castDist > 0.001f)
            {
                // Sphere cast from target toward camera
                if (Physics.SphereCast(from, collisionRadius, delta.normalized, out RaycastHit hit, castDist, collisionLayers, QueryTriggerInteraction.Ignore))
                {
                    float safeDist = Mathf.Max(0f, hit.distance - collisionSkin);
                    desiredPos = from + delta.normalized * safeDist;
                }
            }
        }

        // Smooth position
        Vector3 newPos = Vector3.SmoothDamp(transform.position, desiredPos, ref _vel,
            positionDamping > 0f ? (1f / positionDamping) : 0f);

        transform.position = newPos;

        // Smooth rotation toward lookPoint using slerp
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
