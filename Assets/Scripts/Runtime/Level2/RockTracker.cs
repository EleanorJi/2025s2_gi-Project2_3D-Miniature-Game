using UnityEngine;

public class RockTracker : MonoBehaviour
{
    public RockSurface currentRock      { get; private set; }
    public RockSurface lastJumpFromRock { get; private set; }

    /// Call this when you jump; walking onto a new rock will also auto-update.
    public void MarkJump() { if (currentRock) lastJumpFromRock = currentRock; }

    void OnCollisionEnter(Collision c)
    {
        var r = c.collider.GetComponentInParent<RockSurface>() ?? c.collider.GetComponent<RockSurface>();
        if (!r && !c.collider.CompareTag("Rock")) return;  // If it’s neither the component nor tagged Rock, ignore
        if (!r) r = c.collider.GetComponentInParent<RockSurface>() ?? c.collider.GetComponent<RockSurface>();

        if (r)
        {
            if (currentRock != null && currentRock != r)
                lastJumpFromRock = currentRock;     //Switching rocks: old one becomes “last rock”
            currentRock = r;

            if (lastJumpFromRock == null)
                lastJumpFromRock = currentRock;     //First landing seed so even first honey fall can bounce back
        }
    }

    void OnCollisionExit(Collision c)
    {
        var r = c.collider.GetComponentInParent<RockSurface>() ?? c.collider.GetComponent<RockSurface>();
        if (r && r == currentRock) currentRock = null;
        // Note: Exit does NOT touch lastJumpFromRock
    }
}
