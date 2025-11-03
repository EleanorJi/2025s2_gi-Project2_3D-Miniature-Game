using UnityEngine;

public class MinionAnchor : MonoBehaviour
{

    public static void KillAll()
    {
        var minions = FindObjectsOfType<MinionAnchor>();
        foreach (var m in minions)
        {
            if (m != null)
            {
                Destroy(m.gameObject);
            }
        }
    }

    [Header("Follow Target")]
    public Transform follow;                // player
    public Vector3 localOffset;             // Offset around the player (set during summoning)
    public float stickSpeed = 20f;          // The larger, the tighter the follow

    [Header("Ground Snap")]
    public bool snapToGround = true;
    public float rayHeight = 3f;            // Starting height from above
    public float extraGroundOffset = 0.02f; // Slightly raised to avoid sticking
    public LayerMask groundMask = ~0;       // Only check Ground

    Collider col;
    float footExtentY = 0.1f;

    void Awake()
    {
        col = GetComponent<Collider>();
        UpdateFootExtent();
    }

    void OnValidate()
    {
        // Update in-editor parameters as well
        if (!Application.isPlaying)
        {
            col = GetComponent<Collider>();
            UpdateFootExtent();
        }
    }

    void UpdateFootExtent()
    {
        if (col != null)
        {
            // Ensure a minimum foot extent to avoid sinking into the ground
            footExtentY = Mathf.Max(col.bounds.extents.y, 0.05f);
        }
    }

    void LateUpdate()
    {
        if (!follow) return;

        // Base position
        Vector3 basePos = follow.position + localOffset;

        float desiredY = basePos.y;

        if (snapToGround)
        {
            // Raycast downwards to find the ground
            Vector3 rayStart = basePos + Vector3.up * rayHeight;
            float castDist = rayHeight + 5f;

            bool hit = Physics.Raycast(rayStart, Vector3.down,
                                       out RaycastHit hitInfo, castDist,
                                       groundMask, QueryTriggerInteraction.Ignore);

            // If not hit, try a small sphere cast to avoid missing thin ground
            if (!hit)
            {
                hit = Physics.SphereCast(rayStart, 0.15f, Vector3.down,
                                         out hitInfo, castDist,
                                         groundMask, QueryTriggerInteraction.Ignore);
            }

            if (hit)
            {
                desiredY = hitInfo.point.y + footExtentY + extraGroundOffset;
            }
        }

        Vector3 desired = new Vector3(basePos.x, desiredY, basePos.z);

        // Smoothly interpolate to avoid jitter
        float t = 1f - Mathf.Exp(-stickSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desired, t);
    }
}
