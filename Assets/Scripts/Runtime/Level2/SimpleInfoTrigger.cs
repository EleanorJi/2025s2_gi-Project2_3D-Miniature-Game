using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SimpleInfoTrigger : MonoBehaviour
{
    public string playerTag = "Player";

    [TextArea(2, 5)]
    public string message;          // One sentence of information

    [Header("Once-per-session")]
    [Tooltip("Give this trigger a globally unique ID; same ID will only play once in this game run")]
    public string infoId = "";      // Recommended to fill manually: e.g., "L1_bridge_tip" / "L2_pheromone"

    [Tooltip("When ID not manually filled, auto-generate from hierarchy path (scene name/parent-child hierarchy/object name)")]
    public bool autoIdFromHierarchy = true;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
        if (string.IsNullOrEmpty(infoId)) infoId = BuildAutoId();
    }

    void OnValidate()
    {
        if (autoIdFromHierarchy && string.IsNullOrEmpty(infoId))
            infoId = BuildAutoId();
    }

    string BuildAutoId()
    {
        // Use scene name + hierarchy path, ensures stability (if hierarchy/naming doesn't change)
        return $"{gameObject.scene.name}/{GetHierarchyPath(transform)}";
    }

    static string GetHierarchyPath(Transform t)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder(t.name);
        while (t.parent != null)
        {
            t = t.parent;
            sb.Insert(0, t.name + "/");
        }
        return sb.ToString();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (string.IsNullOrEmpty(message)) return;

        // —— Already played this session? Skip directly —— //
        if (SimpleInfoSession.I.Has(infoId)) return;

        // Mark as played (prevent repeated triggering when death occurs right after popup)
        SimpleInfoSession.I.MarkShown(infoId);

        // Play once
        if (SimpleInfoPopup.I != null)
        {
            SimpleInfoPopup.I.ShowOnce(message);
        }
    }
}
