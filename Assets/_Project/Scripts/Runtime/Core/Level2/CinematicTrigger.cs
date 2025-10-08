using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CinematicTrigger : MonoBehaviour
{
    public string playerTag = "Player";
    public CameraCinematicSequence cinematic;   // 拖你的 Camera（带脚本）进来
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
                Debug.LogWarning("[CinematicTrigger] 没找到 CameraCinematicSequence");
                return;
            }
        }
        cinematic.Play();
        _fired = true;
    }
}
