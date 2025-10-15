using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class SpiderDeathZone : MonoBehaviour
{
    [Header("Filter")]
    public string playerTag = "Player";

    [Header("Death UI (override per zone)")]
    [TextArea] public string overrideMessage;
    public Sprite overrideSprite;
    [Tooltip("<=0 uses template default duration")]
    public float holdSeconds = 4f;

    [Header("Integrate with Water Area (optional)")]
    [Tooltip("只在需要把这次死亡当作“大水域内死亡”时勾上；其他区域保持关闭")]
    public bool enableWaterAreaIntegration = false;

    [Tooltip("推荐手动指定对应的 FloodSequence；留空且启用自动查找时，会按包含关系自动寻找")]
    public FloodSequence waterArea;

    [Tooltip("未手动指定时，是否自动查找“包含当前位置”的 FloodSequence.startZone")]
    public bool autoFindContainingArea = true;

    bool _busy;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_busy) return;
        if (!other.CompareTag(playerTag)) return;

        // 找到玩家控制器（刚体在谁身上就取谁的根）
        var root = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform;
        var pc = root.GetComponent<PlayerController>();
        if (!pc) return;

        _busy = true;

        // —— 在真正 Die() 之前，先把这次死亡标记给“所在的大水域” —— //
        if (enableWaterAreaIntegration)
        {
            var area = waterArea;
            if (!area && autoFindContainingArea)
                area = FindFloodAreaContaining(root.position);

            // 只要当前确实在该水域的 startZone 内，就标记“这轮在水域内死亡”
            if (area != null && area.IsPlayerInsideZonePublic())
            {
                // 只做“标记 + 计数”，不在这里清零/复位；保持由 FloodSequence 的“离开区域”分支统一处理
                area.NotifyPlayerDiedInside();
            }
        }

        // —— 1) 真正杀死玩家（你的通用死亡流程） —— //
        pc.Die();
        GlobalSfx.PlayDeathSfx();

        // —— 2) 弹死亡UI（可覆写文案/图片/时长） —— //
        if (DeathUIOverlay.Instance)
        {
            if (holdSeconds > 0f)
                DeathUIOverlay.Instance.Show(overrideMessage, overrideSprite, holdSeconds);
            else
                DeathUIOverlay.Instance.Show(overrideMessage, overrideSprite, null);
        }

        // 避免同帧多次触发
        StartCoroutine(ClearBusyNextFrame());
    }

    IEnumerator ClearBusyNextFrame() { yield return null; _busy = false; }

    // 在场景里按“包含关系”自动找到 FloodSequence（基于 startZone 的 ClosestPoint）
    FloodSequence FindFloodAreaContaining(Vector3 pos)
    {
        var all = FindObjectsByType<FloodSequence>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var fs in all)
        {
            if (!fs || fs.startZone == null) continue;
            var cp = fs.startZone.ClosestPoint(pos);
            if ((cp - pos).sqrMagnitude < 1e-6f) return fs; // 点在该触发体内
        }
        return null;
    }
}
