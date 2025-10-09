using UnityEngine;

/// <summary>
/// Attach on the Player: press C to pick up a nearby leaf, carry it at original size at carryPoint;
/// pass through a Gate to teleport; after landing, the first movement auto-drops it from dropPoint.
/// Facing rule: while carried X = -90, Z = 90; when dropping, add +90° on X based on current pose.
/// </summary>
public class ParachuteCarrier : MonoBehaviour
{
    [Header("Input")]
    public KeyCode pickupKey = KeyCode.C;

    [Header("Refs (auto-fetched from PlayerController)")]
    public Transform carryPoint;
    public Transform dropPoint;

    [Header("Pose while carried (fine-tune in Inspector)")]
    public Vector3 attachLocalEuler = new Vector3(-90f, 0f, 90f);
    public Vector3 attachLocalOffset = Vector3.zero;

    [Header("Drop position tweak")]
    public float dropYOffset = 0.03f; // Raise world Y a bit on drop

    // State
    ParachuteLeafPickup nearbyLeaf;   // Mark if a leaf is in pickup range
    Transform carriedLeaf;            // The leaf being carried
    Rigidbody carriedLeafRb;
    Collider[] carriedLeafCols;

    // Land -> first movement will drop the leaf
    PlayerController pc;
    bool wasGroundedLastFrame;
    bool waitingForFirstMoveAfterLand;

    void Awake()
    {
        pc = GetComponent<PlayerController>();
        if (pc)
        {
            if (!carryPoint) carryPoint = pc.carryPoint;
            if (!dropPoint)  dropPoint  = pc.dropPoint;
        }
        if (!carryPoint) Debug.LogWarning("[ParachuteCarrier] 缺少 carryPoint");
        if (!dropPoint)  Debug.LogWarning("[ParachuteCarrier] 缺少 dropPoint");
    }

    void Update()
    {
        // Press C to pick up
        if (Input.GetKeyDown(pickupKey))
        {
            if (!carriedLeaf && nearbyLeaf)
                AttachLeaf(nearbyLeaf.transform);
        }

        // Landed -> wait for the first movement to drop the leaf
        bool groundedNow = pc ? (pc.groundContactCount > 0) : false;
        if (!wasGroundedLastFrame && groundedNow)
            waitingForFirstMoveAfterLand = carriedLeaf != null;

        if (waitingForFirstMoveAfterLand && carriedLeaf)
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
            {
                DropLeaf();
                waitingForFirstMoveAfterLand = false;
            }
        }

        wasGroundedLastFrame = groundedNow;
    }

    // Called by the leaf trigger
    public void SetNearbyLeaf(ParachuteLeafPickup leaf) => nearbyLeaf = leaf;
    public ParachuteLeafPickup GetNearbyLeaf() => nearbyLeaf;

    public bool HasLeaf() => carriedLeaf != null;
    // Back-compat with old usages
    public bool hasLeaf() => HasLeaf();
    public void MarkUsedParachute() => DropLeaf();

    // —— Snap onto carryPoint (keep original WORLD size, and force a specific pose) ——
    void AttachLeaf(Transform leaf)
    {
        if (!carryPoint || !leaf) return;

        // Record original world scale (so size stays the same)
        Vector3 worldScale = leaf.lossyScale;

        // Disable physics/collisions
        carriedLeafRb = leaf.GetComponent<Rigidbody>();
        if (carriedLeafRb)
        {
            carriedLeafRb.isKinematic = true;
            carriedLeafRb.useGravity = false;
            carriedLeafRb.linearVelocity  = Vector3.zero;
            carriedLeafRb.angularVelocity = Vector3.zero;
        }
        carriedLeafCols = leaf.GetComponentsInChildren<Collider>(includeInactive: true);
        foreach (var c in carriedLeafCols) c.enabled = false;

        // Parent under carryPoint (don’t keep world pose so we can set local transform directly)
        leaf.SetParent(carryPoint, worldPositionStays: false);

        // Restore world scale: localScale = worldScale / parent.lossyScale
        Vector3 pLossy = carryPoint.lossyScale;
        leaf.localScale = new Vector3(
            worldScale.x / (Mathf.Approximately(pLossy.x, 0f) ? 1f : pLossy.x),
            worldScale.y / (Mathf.Approximately(pLossy.y, 0f) ? 1f : pLossy.y),
            worldScale.z / (Mathf.Approximately(pLossy.z, 0f) ? 1f : pLossy.z)
        );

        // Set the "carried" local position & rotation
        leaf.localPosition = attachLocalOffset;
        leaf.localRotation = Quaternion.Euler(attachLocalEuler);

        carriedLeaf = leaf;

        //SFX: play once on successful attach (same sound as pickup/drop)
        GlobalSfx.PlayLeafPickupSfx(transform.position);
    }

    // —— Re-pin pose every frame, so other scripts/physics won't mess it up ——
    void LateUpdate()
    {
        if (carriedLeaf && carryPoint)
        {
            carriedLeaf.localPosition = attachLocalOffset;
            carriedLeaf.localRotation = Quaternion.Euler(attachLocalEuler);
        }
    }

    // —— Drop at dropPoint, then add +90° to local X based on the current pose, and raise Y by dropYOffset ——
    public void DropLeaf()
    {
        if (!carriedLeaf) return;

        Transform leaf = carriedLeaf;
        carriedLeaf = null;

        // Unparent (keep current world pose)
        leaf.SetParent(null, true);

        // Move to dropPoint (if set; otherwise keep current position)
        if (dropPoint)
        {
            leaf.position = dropPoint.position;
            leaf.rotation = dropPoint.rotation;
        }

        // Add +90° around its own X axis based on current pose
        leaf.Rotate(90f, 0f, 0f, Space.Self);

        //Fix: raise on Y (you previously used Vector3.left by mistake)
        if (!Mathf.Approximately(dropYOffset, 0f))
        {
            leaf.position += Vector3.up * dropYOffset;
        }

        // Restore physics/collisions
        if (carriedLeafRb)
        {
            carriedLeafRb.isKinematic = false;
            carriedLeafRb.useGravity = true;
            carriedLeafRb.linearVelocity = Vector3.zero;
        }
        if (carriedLeafCols != null)
            foreach (var c in carriedLeafCols) c.enabled = true;

        carriedLeafRb = null;
        carriedLeafCols = null;

        //SFX: play the same sound on drop
        GlobalSfx.PlayLeafPickupSfx(transform.position);
    }
}
