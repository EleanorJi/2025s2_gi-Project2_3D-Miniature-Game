using UnityEngine;
using System.Collections.Generic;

public class PressurePlateController : MonoBehaviour
{
    [Header("Layers & Tags")]
    [SerializeField] private string buttonTriggerLayerName = "ButtonTrigger";
    [SerializeField] private string sugarTag = "Sugar";

    [Header("Button Movement Settings")]
    [SerializeField] private float pressDistance = 0.1f; // The distance of the button press
    [SerializeField] private float pressSpeed = 2f; // Press the speed button

    [Header("Object Visibility Control")]
    [SerializeField] private GameObject objectToHide; // The object to be hidden

    [Header("Checkpoint Control")]
    [SerializeField] private Checkpoint checkpoint; // The checkpoint to be controlled (Checkpoint 2.5.1)

    public System.Action<bool> OnPlateActivated;
    public System.Action<GameObject> OnSugarPlaced;

    private int buttonTriggerLayer;
    private readonly HashSet<GameObject> sugarsOnPlate = new();   // The sugar currently on the board
    private readonly HashSet<GameObject> announcedPlaced = new(); // The sugar that has been announced as "placed successfully"
    
    // Variables related to button movement
    private Vector3 originalPosition;
    private Vector3 pressedPosition;
    private bool isPressed = false;
    private float pressLerpTime = 0f;

    private void Awake()
    {
        buttonTriggerLayer = LayerMask.NameToLayer(buttonTriggerLayerName);
        if (buttonTriggerLayer == -1)
        {
            Debug.LogWarning($"[PressurePlate] Layer '{buttonTriggerLayerName}' not found. " +
                             $"Create it and assign it to the small trigger collider under Sugar.");
        }
        
        // Record the original position and calculate the pressed position
        originalPosition = transform.position;
        pressedPosition = originalPosition - Vector3.up * pressDistance;
    }

    private void Update()
    {
        // Smooth movement button
        if (isPressed && pressLerpTime < 1f)
        {
            pressLerpTime += Time.deltaTime * pressSpeed;
            transform.position = Vector3.Lerp(originalPosition, pressedPosition, pressLerpTime);
        }
        else if (!isPressed && pressLerpTime > 0f)
        {
            pressLerpTime -= Time.deltaTime * pressSpeed;
            transform.position = Vector3.Lerp(originalPosition, pressedPosition, pressLerpTime);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsSugarTrigger(other, out var sugar)) return;

        sugarsOnPlate.Add(sugar);
        if (sugarsOnPlate.Count == 1)
        {
            SetPressedState(true);
            OnPlateActivated?.Invoke(true);
            HideObject(); // When the pressure plate is activated, the hidden object is revealed.
            
            // When the sugar cube is placed on the pressure plate, stop the rendering of the checkpoint.
            if (checkpoint != null)
            {
                checkpoint.isActivated = false;
                Debug.Log($"[PressurePlate] Checkpoint {checkpoint.name} deactivated (isActivated = false)");
            }
            
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
            SetPressedState(false);
            OnPlateActivated?.Invoke(false);
            ShowObject(); // When the pressure plate is reset, the object is displayed.
            
            // When the sugar cube leaves the pressure plate, re-activate the rendering of the checkpoint.
            if (checkpoint != null)
            {
                checkpoint.isActivated = true;
                Debug.Log($"[PressurePlate] Checkpoint {checkpoint.name} reactivated (isActivated = true)");
            }
            
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

    private void SetPressedState(bool pressed)
    {
        isPressed = pressed;
    }

    // The method of hiding an object
    private void HideObject()
    {
        if (objectToHide != null)
        {
            objectToHide.SetActive(false);
        }
    }

    // The method of displaying objects
    private void ShowObject()
    {
        if (objectToHide != null)
        {
            objectToHide.SetActive(true);
        }
    }

    // When modifying parameters in Inspector, the pressed position is updated in real time.
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            pressedPosition = originalPosition - Vector3.up * pressDistance;
        }
    }
}