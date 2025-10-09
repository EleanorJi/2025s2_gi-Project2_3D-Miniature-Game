using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SpiderDeathZone : MonoBehaviour
{
    [Header("Filter")]
    public string playerTag = "Player";

    [Header("What to show this time (leave empty = use template default)")]
    [TextArea]
    public string overrideMessage;
    public Sprite overrideSprite;

    [Header("Display duration this time (empty/<=0 uses template default)")]
    public float holdSeconds = 4f;

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

        // 1) Actually kill the player first (they'll go back to save/respawn)
        pc.Die();
        GlobalSfx.PlayDeathSfx();

        // 2) Trigger the global death UI (this time we can override text/sprite/duration)
        if (DeathUIOverlay.Instance)
        {
            if (holdSeconds > 0f)
                DeathUIOverlay.Instance.Show(overrideMessage, overrideSprite, holdSeconds);
            else
                DeathUIOverlay.Instance.Show(overrideMessage, overrideSprite, null); // use default duration
        }

        // Make sure we don't trigger multiple times in a single frame
        StartCoroutine(ClearBusyNextFrame());
    }

    System.Collections.IEnumerator ClearBusyNextFrame()
    {
        yield return null;
        _busy = false;
    }
}
