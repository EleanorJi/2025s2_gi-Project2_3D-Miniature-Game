using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SimpleInfoTrigger : MonoBehaviour
{
    public string playerTag = "Player";

    [TextArea(2, 5)]
    public string message;          // 一句科普

    [Header("Once-per-session")]
    [Tooltip("给这个触发器一个全局唯一ID；相同ID在本次游戏运行中只会播放一次")]
    public string infoId = "";      // 建议自己填：比如 "L1_bridge_tip" / "L2_pheromone"

    [Tooltip("不手填ID时，自动用层级路径生成（场景名/父子层级/物体名）")]
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
        // 用场景名 + 层级路径，保证稳定（层级/命名别改的话）
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

        // —— 本轮已播过？直接跳过 —— //
        if (SimpleInfoSession.I.Has(infoId)) return;

        // 记为已播（防止刚弹出就死亡反复触发）
        SimpleInfoSession.I.MarkShown(infoId);

        // 播放一次
        if (SimpleInfoPopup.I != null)
        {
            SimpleInfoPopup.I.ShowOnce(message);
        }
    }
}
