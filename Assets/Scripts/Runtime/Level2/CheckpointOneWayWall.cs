using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CheckpointOneWayWall : MonoBehaviour
{
    [Header("Who to lock")]
    public string playerTag = "Player";

    [Header("The air wall to enable")]
    [Tooltip("Choose one: directly enable this Collider…")]
    public Collider wallCollider;
    [Tooltip("…or activate this entire wall GameObject")]
    public GameObject wallObject;

    [Header("Behavior")]
    [Tooltip("Trigger only once per session (this run)")]
    public bool oncePerSession = true;
    [Tooltip("Globally unique ID for this trigger (empty will auto-use hierarchy path)")]
    public string checkpointId = "";

    [Tooltip("Temporarily ignore collision between player and wall before enabling wall, to avoid trapping player in wall")]
    public float ignoreCollisionSeconds = 0.2f;

    // === Forward Direction Settings ===
    public enum DirectionMode { UseTransformForward, UseAxis, UseCustomVector }
    [Header("Forward Direction")]
    public DirectionMode directionMode = DirectionMode.UseTransformForward;

    [Tooltip("Effective when UseAxis is selected")]
    public Axis axis = Axis.Z;
    public enum Axis { X, Y, Z }

    [Tooltip("Axis is positive (+1) or negative (-1)")]
    public bool positive = true;

    [Tooltip("Effective when UseCustomVector is selected (world coordinates). E.g., (1,0,0) means world +X is forward direction.")]
    public Vector3 customWorldDirection = Vector3.forward;

    [Tooltip("Invert overall direction (equivalent to swapping forward/backward)")]
    public bool invertDirection = false;

    // Session memory
    static HashSet<string> activatedIds = new HashSet<string>();

    // Internal
    bool _armed = true;     // Allow triggering
    int _lastSide = 0;      // Record which side player is on when entering/leaving (-1 / +1)

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
        if (string.IsNullOrEmpty(checkpointId))
            checkpointId = BuildAutoId();
    }

    void Awake()
    {
        if (string.IsNullOrEmpty(checkpointId))
            checkpointId = BuildAutoId();

        // If already triggered this session, directly enable wall (can change to keep closed if needed)
        if (oncePerSession && activatedIds.Contains(checkpointId))
            ActivateWallImmediate();
        else
            DeactivateWallImmediate(); // Ensure initially closed
    }

    void OnTriggerEnter(Collider other)
    {
        if (!_armed || !other.CompareTag(playerTag)) return;
        _lastSide = SideOf(other.transform.position);
    }

    void OnTriggerExit(Collider other)
    {
        if (!_armed || !other.CompareTag(playerTag)) return;
        int nowSide = SideOf(other.transform.position);

        // Only trigger lock when crossing from "back side (-1) → front side (+1)"
        if (_lastSide < 0 && nowSide > 0)
        {
            LockBackPath(other);
        }
    }

    // Calculate player's side relative to "checkpoint line": >0 means on forward direction side, <0 means on back side
    int SideOf(Vector3 worldPos)
    {
        Vector3 fwd = GetForward();
        Vector3 toPoint = worldPos - transform.position;
        float dot = Vector3.Dot(fwd, toPoint);
        return dot >= 0f ? +1 : -1;
    }

    Vector3 GetForward()
    {
        Vector3 fwd;
        switch (directionMode)
        {
            case DirectionMode.UseAxis:
                switch (axis)
                {
                    case Axis.X: fwd = positive ? Vector3.right  : Vector3.left;  break;
                    case Axis.Y: fwd = positive ? Vector3.up     : Vector3.down;  break;
                    default:     fwd = positive ? Vector3.forward: Vector3.back;  break;
                }
                break;

            case DirectionMode.UseCustomVector:
                fwd = customWorldDirection;
                if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.forward; // Fallback
                break;

            default:
                fwd = transform.forward;
                break;
        }
        if (invertDirection) fwd = -fwd;
        return fwd.normalized;
    }

    void LockBackPath(Collider player)
    {
        if (oncePerSession) activatedIds.Add(checkpointId);

        ActivateWallImmediate();

        // Briefly ignore collision between player and wall, avoid getting stuck immediately
        if (wallCollider)
        {
            var playerCol = player; // For precision can grab player's CapsuleCollider/CharacterController
            Physics.IgnoreCollision(playerCol, wallCollider, true);
            StartCoroutine(ReenableCollisionLater(playerCol));
        }

        _armed = false; // No longer trigger repeatedly
    }

    IEnumerator ReenableCollisionLater(Collider playerCol)
    {
        float t = 0f;
        while (t < ignoreCollisionSeconds)
        {
            t += Time.unscaledDeltaTime; // Count time even when paused
            yield return null;
        }
        if (wallCollider && playerCol)
            Physics.IgnoreCollision(playerCol, wallCollider, false);
    }

    void ActivateWallImmediate()
    {
        if (wallObject)   wallObject.SetActive(true);
        if (wallCollider) wallCollider.enabled = true;
    }

    void DeactivateWallImmediate()
    {
        if (wallObject)   wallObject.SetActive(false);
        if (wallCollider) wallCollider.enabled = false;
    }

    string BuildAutoId()
    {
        return $"{gameObject.scene.name}/{GetHierarchyPath(transform)}";
    }
    static string GetHierarchyPath(Transform t)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder(t.name);
        while (t.parent != null)
        {
            t = t.parent;
            sb.Insert(0, t.name + "/");
        }
        return sb.ToString();
    }

    // —— Direction Visualization —— //
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 fwd = Application.isPlaying ? GetForward() : PreviewForwardInEditor();
        Vector3 p = transform.position;
        Gizmos.DrawLine(p, p + fwd * 2.0f);
        DrawArrowHead(p + fwd * 2.0f, fwd);
    }

    Vector3 PreviewForwardInEditor()
    {
        // Can preview direction in inspector before runtime
        Vector3 fwd;
        switch (directionMode)
        {
            case DirectionMode.UseAxis:
                switch (axis)
                {
                    case Axis.X: fwd = positive ? Vector3.right : Vector3.left; break;
                    case Axis.Y: fwd = positive ? Vector3.up    : Vector3.down; break;
                    default:     fwd = positive ? Vector3.forward:Vector3.back; break;
                }
                break;
            case DirectionMode.UseCustomVector:
                fwd = customWorldDirection.sqrMagnitude < 1e-6f ? Vector3.forward : customWorldDirection;
                break;
            default:
                fwd = transform.forward;
                break;
        }
        if (invertDirection) fwd = -fwd;
        return fwd.normalized;
    }

    void DrawArrowHead(Vector3 tip, Vector3 dir)
    {
        Vector3 right = Quaternion.AngleAxis(25f, Vector3.up) * (-dir);
        Vector3 left  = Quaternion.AngleAxis(-25f, Vector3.up) * (-dir);
        Gizmos.DrawLine(tip, tip + right * 0.4f);
        Gizmos.DrawLine(tip, tip + left  * 0.4f);
    }
}
