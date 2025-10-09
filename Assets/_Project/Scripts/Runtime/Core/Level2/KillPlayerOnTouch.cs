using UnityEngine;

public class KillPlayerOnTouch : MonoBehaviour
{
    [Header("Death UI (you can override in this component)")]
    [TextArea] public string deathMessage = "You got bugged out...";
    public Sprite deathSprite;
    public float deathDuration = 4f; // <=0 uses the template's default duration

    // Prevent double-trigger when both OnCollision and OnTrigger fire on the same contact
    bool _busy;

    void TriggerDeath(GameObject go)
    {
        if (_busy || go == null) return;

        var pc = go.GetComponent<PlayerController>();
        if (!pc) return;

        _busy = true;

        // First, run your existing death flow (respawn, etc.)
        pc.Die();
        GlobalSfx.PlayDeathSfx();

        // Then show the global death UI template (text/sprite/duration can be overridden here; leave blank to use defaults)
        DeathUIOverlay.Instance?.Show(
            string.IsNullOrEmpty(deathMessage) ? null : deathMessage,
            deathSprite,
            (deathDuration > 0f) ? deathDuration : (float?)null
        );

        // Brief cooldown to avoid retriggering within the same frame/short window
        Invoke(nameof(ResetBusy), 0.2f);
    }

    void ResetBusy() => _busy = false;

    private void OnCollisionEnter(Collision c)
    {
        if (c.collider.CompareTag("Player"))
            TriggerDeath(c.collider.gameObject);
    }

    // Safety net: if some collider is set to Trigger, still handle it
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            TriggerDeath(other.gameObject);
    }
}
