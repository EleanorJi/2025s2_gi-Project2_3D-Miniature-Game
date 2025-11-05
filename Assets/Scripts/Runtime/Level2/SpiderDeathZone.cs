using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class SpiderDeathZone : MonoBehaviour
{
    [Header("Filter")]
    public string playerTag = "Player";

    [Header("Death UI (override per zone)")]
    [TextArea] public string overrideMessage;
    public Sprite overrideSprite;
    [Tooltip("<=0 uses template default duration")]
    public float holdSeconds = 4f;

    [Header("Integrate with Water Area (optional)")]
    [Tooltip("Only enable when this death should be treated as 'death inside large water area'; keep disabled for other zones")]
    public bool enableWaterAreaIntegration = false;

    [Tooltip("Recommended to manually specify the corresponding FloodSequence; if empty and auto-find is enabled, will auto-search by containment")]
    public FloodSequence waterArea;

    [Tooltip("When not manually specified, whether to auto-find FloodSequence.startZone that contains current position")]
    public bool autoFindContainingArea = true;

    [Header("Respawn Override (optional)")]
    [Tooltip("If specified: deaths in this zone will force respawn at this point (overrides global checkpoint). Leave empty: use normal checkpoint flow.")]
    public Transform respawnOverride;

    [Tooltip("If a global RespawnManager exists, can override 'next respawn point' before Die(). Leave empty to ignore.")]
    public RespawnManager respawnManager; // Optional: respawn manager in your project (if exists)
    
    [Header("Routing Preference")]
    [Tooltip("If enabled, when hitting water area, directly calls FloodSequence.HandleDeathInsideExternal(), letting water area handle teleport/reset uniformly.")]
    public bool preferFloodAreaHandle = true;

    bool _busy;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_busy) return;
        if (!other.CompareTag(playerTag)) return;

        var root = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform;
        var pc = root.GetComponent<PlayerController>();
        if (!pc) return;

        _busy = true;

        // —— Priority check: if dedicated respawn point is set, use it directly (overrides water area and checkpoint) —— //
        if (respawnOverride)
        {
            // Directly teleport to specified respawn point
            SafeWarpImmediate(root, respawnOverride);
            
            // Play death effect
            GlobalSfx.PlayDeathSfx();
            if (DeathUIOverlay.Instance)
            {
                if (holdSeconds > 0f)
                    DeathUIOverlay.Instance.Show(overrideMessage, overrideSprite, holdSeconds);
                else
                    DeathUIOverlay.Instance.Show(overrideMessage, overrideSprite, null);
            }
            
            StartCoroutine(ClearBusyNextFrame());
            return;
        }

        // —— Try to locate current large water area (if integration enabled) —— //
        FloodSequence area = null;
        if (enableWaterAreaIntegration)
        {
            area = waterArea;
            if (!area && autoFindContainingArea)
                area = FindFloodAreaContaining(root.position);
        }

        // —— Strategy A: Priority to let FloodSequence handle "internal death processing" —— //
        // Condition: integration enabled and actually inside the water area, and prefer water area handling (avoid being teleported elsewhere by global checkpoint)
        if (preferFloodAreaHandle && area != null && area.IsPlayerInsideZonePublic())
        {
            // Let large water area execute: mark death in water + reset cookies + reset flood + teleport to waterArea.respawnPoint
            area.HandleDeathInsideExternal();

            // Only perform effects (SFX + death UI), don't call pc.Die() to avoid triggering global checkpoint intervention
            GlobalSfx.PlayDeathSfx();
            if (DeathUIOverlay.Instance)
            {
                if (holdSeconds > 0f)
                    DeathUIOverlay.Instance.Show(overrideMessage, overrideSprite, holdSeconds);
                else
                    DeathUIOverlay.Instance.Show(overrideMessage, overrideSprite, null);
            }

            StartCoroutine(ClearBusyNextFrame());
            return;
        }

        // —— Strategy B: Execute general "death" flow (handled by global lifecycle) —— //
        pc.Die();
        GlobalSfx.PlayDeathSfx();

        if (DeathUIOverlay.Instance)
        {
            if (holdSeconds > 0f)
                DeathUIOverlay.Instance.Show(overrideMessage, overrideSprite, holdSeconds);
            else
                DeathUIOverlay.Instance.Show(overrideMessage, overrideSprite, null);
        }

        // —— If water area integration is enabled but Strategy A was not used, at least record "died this round" in water area (not responsible for teleport) —— //
        if (enableWaterAreaIntegration && area != null && area.IsPlayerInsideZonePublic())
        {
            area.NotifyPlayerDiedInside();
        }

        StartCoroutine(ClearBusyNextFrame());
    }

    IEnumerator ClearBusyNextFrame() { yield return null; _busy = false; }

    // Auto-find FloodSequence in scene by "containment relationship" (based on startZone's ClosestPoint)
    FloodSequence FindFloodAreaContaining(Vector3 pos)
    {
        var all = FindObjectsByType<FloodSequence>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var fs in all)
        {
            if (!fs || fs.startZone == null) continue;
            var cp = fs.startZone.ClosestPoint(pos);
            if ((cp - pos).sqrMagnitude < 1e-6f) return fs; // Point is inside the trigger
        }
        return null;
    }

    // Safe teleport (handles RB / CC / Agent), used as fallback for Strategy B's "move first then die"
    void SafeWarpImmediate(Transform t, Transform target)
    {
        if (!t || !target) return;

        var rb = t.GetComponent<Rigidbody>();
        var cc = t.GetComponent<CharacterController>();
        var agent = t.GetComponent<UnityEngine.AI.NavMeshAgent>();

        bool ccWas = false, agentWas = false;
        if (cc) { ccWas = cc.enabled; cc.enabled = false; }
        if (agent){ agentWas = agent.enabled; agent.enabled = false; }

        if (rb)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        t.SetPositionAndRotation(target.position, target.rotation);

        if (rb)
        {
            rb.isKinematic = false;
            rb.WakeUp();
        }
        if (cc) cc.enabled = ccWas;
        if (agent) agent.enabled = agentWas;

        Physics.SyncTransforms();
    }
}

// —— Just an example interface; if your project already has a manager with different name/signature, drag it in Inspector and adapt —— //
public class RespawnManager : MonoBehaviour
{
    // Interface for SpiderDeathZone to optionally call "next respawn point override"
    public void OverrideNextRespawn(Vector3 pos, Quaternion rot)
    {
        // TODO: Implement your project's "next respawn point" setting (e.g., store in static field, clear after respawn)
    }
}
