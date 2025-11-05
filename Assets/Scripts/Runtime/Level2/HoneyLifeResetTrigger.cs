using UnityEngine;

/// Attach it to a small BoxCollider (IsTrigger) positioned at the “respawn point”
/// Function: When the player respawns or passes by, reset the “Lifeline Honey Counter” to 0 (granting two more self-rescue opportunities)[RequireComponent(typeof(Collider))]
public class HoneyLifeResetTrigger : MonoBehaviour
{
    public bool onlyWhenPlayerTag = true;
    public string playerTag = "Player";
    bool _doneThisFrame;

    void Reset() { GetComponent<Collider>().isTrigger = true; }

    void LateUpdate() { _doneThisFrame = false; } // Allow triggering again next frame

    void OnTriggerEnter(Collider other) { TryReset(other); }
    void OnTriggerStay(Collider other)  { TryReset(other); }

    void TryReset(Collider other)
    {
        if (_doneThisFrame) return;
        var t = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform;
        if (onlyWhenPlayerTag && !t.CompareTag(playerTag)) return;

        StickyGooMashR.ResetGlobalHoneyCounter();
        _doneThisFrame = true;
        
    }
}
