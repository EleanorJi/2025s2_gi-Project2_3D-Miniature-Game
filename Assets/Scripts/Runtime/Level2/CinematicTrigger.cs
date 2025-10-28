using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CinematicTrigger : MonoBehaviour
{
    public string playerTag = "Player";
    public CameraCinematicSequence cinematic;
    public bool oneShot = true;

    bool _fired = false;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_fired && oneShot) return;
        if (!other.CompareTag(playerTag)) return;
        if (!cinematic)
        {
            cinematic = FindFirstObjectByType<CameraCinematicSequence>();
            if (!cinematic)
            {
                Debug.LogWarning("[CinematicTrigger] cannot find CameraCinematicSequence");
                return;
            }
        }
        cinematic.Play();
        _fired = true;
    }
}
