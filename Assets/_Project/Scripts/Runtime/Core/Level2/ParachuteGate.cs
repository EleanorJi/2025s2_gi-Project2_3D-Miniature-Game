using UnityEngine;

/// <summary>
/// 花圃边“离开判定”触发区：玩家离开本区时检查是否携带叶子。
/// - 有叶子：用掉叶子，并把玩家传送到下一小关起点（或安全落点）。
/// - 没叶子：立刻死亡，并弹出可配置的死亡UI（使用全局 DeathUIOverlay 模板）。
///
/// 用法：
/// 1) 将本脚本挂到一个 Collider 上并勾选 IsTrigger；把触发区放在“边缘外沿”，确保玩家离开时会触发 OnTriggerExit。
/// 2) 设定 nextStartPoint 为“下一小关的起点/落点”的 Transform。
/// 3) Player 需要有 ParachuteCarrier 和 PlayerController。
/// </summary>
[RequireComponent(typeof(Collider))]
public class ParachuteGate : MonoBehaviour
{
    [Header("过滤")]
    public string requiredTag = "Player";

    [Header("到达位置（必填）")]
    public Transform nextStartPoint;

    [Header("行为")]
    public bool consumeLeaf = true;           // 通过时是否消耗叶子
    public bool killIfNoLeaf = true;          // 没叶子是否直接死亡
    public bool snapRotation = true;          // 传送时是否对齐朝向
    public Vector3 snapOffset = Vector3.zero; // 传送附加偏移（可微调落点）

    [Header("死亡 UI（无叶子时显示，可在组件里自定义）")]
    [TextArea] public string deathMessage = "没有叶子减速，摔死了…";
    public Sprite deathSprite;
    public float deathDuration = 4f;          // <=0 则使用模板默认时长

    // 防抖：避免同一帧多次触发（例如角色多个碰撞体）
    bool _busy;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (_busy) return;
        if (!other.CompareTag(requiredTag)) return;

        var carrier = other.GetComponent<ParachuteCarrier>();
        var player  = other.GetComponent<PlayerController>();
        if (!carrier || !player)
        {
            Debug.LogWarning("[ParachuteGate] Player 缺少 ParachuteCarrier 或 PlayerController。");
            return;
        }

        _busy = true;

        if (carrier.HasLeaf())
        {
            // 有叶子：安全传送到下一段
            if (nextStartPoint)
            {
                var targetPos = nextStartPoint.position + snapOffset;
                other.transform.position = targetPos;
                if (snapRotation) other.transform.rotation = nextStartPoint.rotation;
            }
            else
            {
                Debug.LogWarning("[ParachuteGate] 未设置 nextStartPoint，无法传送。");
            }

            if (consumeLeaf)
                carrier.DropLeaf(); // 用掉叶子
        }
        else
        {
            // 没叶子：摔死 + 弹死亡UI
            if (killIfNoLeaf)
            {
                player.Die();

                // 调用全局模板（可在本组件 Inspector 覆盖文字/图片/时长）
                DeathUIOverlay.Instance?.Show(
                    string.IsNullOrEmpty(deathMessage) ? null : deathMessage,
                    deathSprite,
                    (deathDuration > 0f) ? deathDuration : (float?)null
                );
            }
        }

        // 小冷却，避免重复触发
        Invoke(nameof(ResetBusy), 0.2f);
    }

    void ResetBusy() => _busy = false;

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // 画目标点
        if (nextStartPoint)
        {
            Gizmos.color = new Color(0.2f, 1f, 0.6f, 0.8f);
            Gizmos.DrawWireSphere(nextStartPoint.position + snapOffset, 0.1f);
            Gizmos.DrawLine(transform.position, nextStartPoint.position + snapOffset);
        }

        // 画触发区（仅 BoxCollider 简单预览）
        var box = GetComponent<BoxCollider>();
        if (box && box.isTrigger)
        {
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.15f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
        }
    }
#endif
}