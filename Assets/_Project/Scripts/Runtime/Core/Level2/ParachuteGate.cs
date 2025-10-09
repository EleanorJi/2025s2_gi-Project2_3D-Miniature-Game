using UnityEngine;

/// <summary>
/// Flowerbed edge "leave check" trigger: when the player leaves this area, check if they carry a leaf.
/// - Has leaf: consume one and teleport the player to the next mini-stage start (or a safe landing).
/// - No leaf: kill immediately and pop the configurable death UI (uses global DeathUIOverlay template).
///
/// Usage:
/// 1) Attach this script to a Collider with IsTrigger checked; place the trigger along the outer edge so OnTriggerExit fires when leaving.
/// 2) Set nextStartPoint to the Transform for the next mini-stage start/landing.
/// 3) Player must have ParachuteCarrier and PlayerController.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ParachuteGate : MonoBehaviour
{
    [Header("Filter")]
    public string requiredTag = "Player";

    [Header("Destination (required)")]
    public Transform nextStartPoint;

    [Header("Behavior")]
    public bool consumeLeaf = true;           // Consume a leaf when passing through
    public bool killIfNoLeaf = true;          // If no leaf, die instantly
    public bool snapRotation = true;          // Align rotation when teleporting
    public Vector3 snapOffset = Vector3.zero; // Extra offset when teleporting (fine-tune landing)

    [Header("Death UI (shown when no leaf; customizable in Inspector)")]
    [TextArea] public string deathMessage = "Without the leaves to slow me down, I crashed to my death...";
    public Sprite deathSprite;
    public float deathDuration = 4f;          // <=0 uses template default duration

    // Debounce: avoid multiple triggers in the same frame (e.g., multiple colliders on the character)
    bool _busy;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (_busy) return;
        if (!other.CompareTag(requiredTag)) return;

        var carrier = other.GetComponent<ParachuteCarrier>();
        var player  = other.GetComponent<PlayerController>();
        if (!carrier || !player)
        {
            Debug.LogWarning("[ParachuteGate] Player no ParachuteCarrier or PlayerController");
            return;
        }

        _busy = true;

        if (carrier.HasLeaf())
        {
            // Has leaf: safe teleport to the next segment
            if (nextStartPoint)
            {
                var targetPos = nextStartPoint.position + snapOffset;
                other.transform.position = targetPos;
                if (snapRotation) other.transform.rotation = nextStartPoint.rotation;
            }
            else
            {
                Debug.LogWarning("[ParachuteGate] no set nextStartPoint");
            }

            if (consumeLeaf)
                carrier.DropLeaf(); // Use up one leaf
        }
        else
        {
            // No leaf: death + show death UI
            if (killIfNoLeaf)
            {
                player.Die();
                GlobalSfx.PlayDeathSfx();

                // Use the global template (you can override text/sprite/duration in this component)
                DeathUIOverlay.Instance?.Show(
                    string.IsNullOrEmpty(deathMessage) ? null : deathMessage,
                    deathSprite,
                    (deathDuration > 0f) ? deathDuration : (float?)null
                );
            }
        }

        // Small cooldown to avoid repeat triggers
        Invoke(nameof(ResetBusy), 0.2f);
    }

    void ResetBusy() => _busy = false;

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // Draw target point
        if (nextStartPoint)
        {
            Gizmos.color = new Color(0.2f, 1f, 0.6f, 0.8f);
            Gizmos.DrawWireSphere(nextStartPoint.position + snapOffset, 0.1f);
            Gizmos.DrawLine(transform.position, nextStartPoint.position + snapOffset);
        }

        // Draw trigger preview (simple BoxCollider preview only)
        var box = GetComponent<BoxCollider>();
        if (box && box.isTrigger)
        {
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.15f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
        }
    }
#endif
}
