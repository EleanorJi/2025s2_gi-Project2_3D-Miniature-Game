using UnityEngine;
using System.Collections.Generic;

public class PressurePlateController : MonoBehaviour
{
    [Header("Layers & Tags")]
    [SerializeField] private string buttonTriggerLayerName = "ButtonTrigger";
    [SerializeField] private string sugarTag = "Sugar";

    [Header("Button Movement Settings")]
    [SerializeField] private float pressDistance = 0.1f; // 按钮按下的距离
    [SerializeField] private float pressSpeed = 2f; // 按下速度

    [Header("Object Visibility Control")]
    [SerializeField] private GameObject objectToHide; // 要隐藏的物体

    public System.Action<bool> OnPlateActivated;
    public System.Action<GameObject> OnSugarPlaced;

    private int buttonTriggerLayer;
    private readonly HashSet<GameObject> sugarsOnPlate = new();   // The sugar currently on the board
    private readonly HashSet<GameObject> announcedPlaced = new(); // The sugar that has been announced as "placed successfully"
    
    // 按钮移动相关变量
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
        
        // 记录原始位置并计算按下位置
        originalPosition = transform.position;
        pressedPosition = originalPosition - Vector3.up * pressDistance;
    }

    private void Update()
    {
        // 平滑移动按钮
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
            HideObject(); // 当压力板被激活时隐藏物体
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
            ShowObject(); // 当压力板复位时显示物体
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

    // 隐藏物体的方法
    private void HideObject()
    {
        if (objectToHide != null)
        {
            objectToHide.SetActive(false);
        }
    }

    // 显示物体的方法
    private void ShowObject()
    {
        if (objectToHide != null)
        {
            objectToHide.SetActive(true);
        }
    }

    // 在Inspector中修改参数时实时更新按下位置
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            pressedPosition = originalPosition - Vector3.up * pressDistance;
        }
    }
}