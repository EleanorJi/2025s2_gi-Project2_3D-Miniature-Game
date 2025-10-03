using UnityEngine;

public class RockSurface : MonoBehaviour
{
    [Tooltip("玩家回到这块石头时落点；放在表面上方一点")]
    public Transform respawnAnchor;

    // 工具：一键在顶面创建锚点（右键组件 → Create Anchor At Top）
    [ContextMenu("Create Anchor At Top")]
    void CreateAnchorAtTop() {
        var t = new GameObject("RespawnAnchor").transform;
        t.SetParent(transform);
        var col = GetComponentInChildren<Collider>();
        var top = col ? col.bounds.center + Vector3.up * (col.bounds.extents.y + 0.2f) : transform.position + Vector3.up * 0.5f;
        t.position = top; t.rotation = transform.rotation;
        respawnAnchor = t;
    }
}
