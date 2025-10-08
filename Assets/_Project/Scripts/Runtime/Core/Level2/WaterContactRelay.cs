using UnityEngine;

public class WaterContactRelay : MonoBehaviour
{
    public string playerTag = "Player";
    public System.Action<Collider> onEnter, onExit;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag)) onEnter?.Invoke(other);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag)) onExit?.Invoke(other);
    }
}
