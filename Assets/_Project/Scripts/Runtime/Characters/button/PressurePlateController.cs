using UnityEngine;
using System.Collections.Generic;

public class PressurePlateController : MonoBehaviour
{
    [Header("Layers & Tags")]
    [SerializeField] private string buttonTriggerLayerName = "ButtonTrigger";
    [SerializeField] private string sugarTag = "Sugar";

    // 旧：板子整体激活/取消（如果别处在用可以继续用）
    public System.Action<bool> OnPlateActivated;
    // 新：某一块糖被“放下”到板子上（只触发一次）
    public System.Action<GameObject> OnSugarPlaced;

    private int buttonTriggerLayer;
    private readonly HashSet<GameObject> sugarsOnPlate = new();   // 当前在板上的糖
    private readonly HashSet<GameObject> announcedPlaced = new(); // 已经宣布“放置成功”的糖

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
        announcedPlaced.Remove(sugar); // 离开后下次再放还能再次触发
        if (sugarsOnPlate.Count == 0)
        {
            OnPlateActivated?.Invoke(false);
            // Debug.Log("Plate deactivated");
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!IsSugarTrigger(other, out var sugar)) return;

        // 方案 B：只看“是否还是玩家体系的子物体”
        bool isCarried = sugar.GetComponentInParent<PlayerController>() != null;

        // 在板上 && 已放下（不是被拿着）&& 这块糖还没宣布过
        if (sugarsOnPlate.Contains(sugar) && !isCarried && !announcedPlaced.Contains(sugar))
        {
            announcedPlaced.Add(sugar);
            OnSugarPlaced?.Invoke(sugar);
            Debug.Log($"[Plate] Sugar placed: {sugar.name}");
        }
    }

    // 工具：从子触发器拿到糖的父物体，并校验层/Tag
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
