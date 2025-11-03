// ParachuteDropZone.cs  新建脚本并挂到“重生点区域”的触发体上
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
