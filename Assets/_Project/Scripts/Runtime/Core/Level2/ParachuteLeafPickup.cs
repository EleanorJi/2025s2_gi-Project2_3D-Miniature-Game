using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ParachuteLeafPickup : MonoBehaviour
{
    
    public GameObject highlight;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
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

        if (carrier.GetNearbyLeaf() == this)
            carrier.SetNearbyLeaf(null);

        if (highlight) highlight.SetActive(false);
    }
}
