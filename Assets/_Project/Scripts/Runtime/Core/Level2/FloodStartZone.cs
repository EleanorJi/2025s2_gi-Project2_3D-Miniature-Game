using UnityEngine;

public class FloodStartZone : MonoBehaviour
{
    public string playerTag = "Player";
    public System.Action onPlayerEnter;
    public System.Action onPlayerExit;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag)) onPlayerEnter?.Invoke();
    }
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag)) onPlayerExit?.Invoke();
    }
}
