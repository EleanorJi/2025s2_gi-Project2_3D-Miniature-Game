// ParachuteDropZone.cs - Attach this script to the trigger collider at the respawn zone
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ParachuteDropZone : MonoBehaviour
{
    public string requiredTag = "Player";
    public PlayerPoisonShooter poisonShooter;
    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(requiredTag)) return;

        var carrier = other.GetComponent<ParachuteCarrier>();
        if (carrier && carrier.HasLeaf())
        {
            poisonShooter.enabled = false ;
            carrier.DropLeaf();
        }
    }
}
