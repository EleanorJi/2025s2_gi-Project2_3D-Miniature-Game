using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ParachuteLeafPickup : MonoBehaviour
{
    // 可选：靠近时高亮
    public GameObject highlight;   // 可留空；靠近时 SetActive(true)

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true; // 作为“可拾取范围”
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        var carrier = other.GetComponent<ParachuteCarrier>();
        if (!carrier) return;

        carrier.SetNearbyLeaf(this);
        if (highlight) highlight.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        var carrier = other.GetComponent<ParachuteCarrier>();
        if (!carrier) return;

        // 只有当前记录的还是我，才清空
        if (carrier.GetNearbyLeaf() == this)
            carrier.SetNearbyLeaf(null);

        if (highlight) highlight.SetActive(false);
    }
}
