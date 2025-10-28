using UnityEngine;

public class RockSurface : MonoBehaviour
{
    [Tooltip("The player's landing point when returning to this stone")]
    public Transform respawnAnchor;

    //（Right Click Component → Create Anchor At Top）
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
