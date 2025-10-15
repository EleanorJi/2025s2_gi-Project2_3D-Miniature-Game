using UnityEngine;
using System.Collections.Generic;

public class PressurePlateController : MonoBehaviour
{
    [Header("Layers & Tags")]
    [SerializeField] private string buttonTriggerLayerName = "ButtonTrigger";
    [SerializeField] private string sugarTag = "Sugar";

    public System.Action<bool> OnPlateActivated;
    public System.Action<GameObject> OnSugarPlaced;

    private int buttonTriggerLayer;
    private readonly HashSet<GameObject> sugarsOnPlate = new();   // The sugar currently on the board
    private readonly HashSet<GameObject> announcedPlaced = new(); // The sugar that has been announced as "placed successfully"

    private void Awake()
    {
        buttonTriggerLayer = LayerMask.NameToLayer(buttonTriggerLayerName);
        if (buttonTriggerLayer == -1)
        {
            Debug.LogWarning($"[PressurePlate] Layer '{buttonTriggerLayerName}' not found. " +
                             $"Create it and assign it to the small trigger collider under Sugar.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsSugarTrigger(other, out var sugar)) return;

        sugarsOnPlate.Add(sugar);
        if (sugarsOnPlate.Count == 1)
        {
            OnPlateActivated?.Invoke(true);
            // Debug.Log("Plate activated");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsSugarTrigger(other, out var sugar)) return;

        sugarsOnPlate.Remove(sugar);
        announcedPlaced.Remove(sugar); // After leaving, it can be triggered again the next time you release it
        if (sugarsOnPlate.Count == 0)
        {
            OnPlateActivated?.Invoke(false);
            // Debug.Log("Plate deactivated");
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!IsSugarTrigger(other, out var sugar)) return;

        bool isCarried = sugar.GetComponentInParent<PlayerController>() != null;

        // On the plate && Placed (not being carried) && This sugar has not been announced yet
        if (sugarsOnPlate.Contains(sugar) && !isCarried && !announcedPlaced.Contains(sugar))
        {
            announcedPlaced.Add(sugar);
            OnSugarPlaced?.Invoke(sugar);
            Debug.Log($"[Plate] Sugar placed: {sugar.name}");
        }
    }

    private bool IsSugarTrigger(Collider other, out GameObject sugarParent)
    {
        sugarParent = null;
        if (other.gameObject.layer != buttonTriggerLayer) return false;

        var p = other.transform.parent ? other.transform.parent.gameObject : null;
        if (p == null || !p.CompareTag(sugarTag)) return false;

        sugarParent = p;
        return true;
    }
}
