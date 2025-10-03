using UnityEngine;

public class RockTracker : MonoBehaviour
{
    public RockSurface currentRock { get; private set; }
    public RockSurface lastJumpFromRock { get; private set; }

    public void MarkJump() { if (currentRock) lastJumpFromRock = currentRock; }

    void OnCollisionEnter(Collision c) {
        if (c.collider.CompareTag("Rock")) {
            var r = c.collider.GetComponentInParent<RockSurface>() ?? c.collider.GetComponent<RockSurface>();
            if (r) currentRock = r;
        }
    }
    void OnCollisionExit(Collision c) {
        if (c.collider.CompareTag("Rock")) {
            var r = c.collider.GetComponentInParent<RockSurface>() ?? c.collider.GetComponent<RockSurface>();
            if (r && r == currentRock) currentRock = null;
        }
    }
}
