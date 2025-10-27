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

    [Header("Respawn Override (optional)")]
    [Tooltip("若指定：本区死亡将强制在此点复活（优先于全局检查点）。留空：按正常 checkpoint 流程。")]
    public Transform respawnOverride;

    [Tooltip("若存在全局 RespawnManager，可在 Die() 前覆盖“下一次重生点”。留空则忽略。")]
    public RespawnManager respawnManager; // 可选：你项目里的重生管理器（如果有）
    
    [Header("Routing Preference")]
    [Tooltip("勾上后，若命中水域，则直接调用 FloodSequence.HandleDeathInsideExternal()，由水域统一处理传送/清零。")]
    public bool preferFloodAreaHandle = true;

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

        var root = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform;
        var pc = root.GetComponent<PlayerController>();
        if (!pc) return;

        _busy = true;

        // —— 尝试定位当前所在的大水域（若开启集成）—— //
        FloodSequence area = null;
        if (enableWaterAreaIntegration)
        {
            area = waterArea;
            if (!area && autoFindContainingArea)
                area = FindFloodAreaContaining(root.position);
        }

        // —— Strategy A：优先交给 FloodSequence 自己“内部处理死亡” —— //
        // 条件：已开启集成且确实在该水域里，并且你更偏好水域处理（避免被全局 checkpoint 传到别处）
        if (preferFloodAreaHandle && area != null && area.IsPlayerInsideZonePublic())
        {
            // 让大水域执行：标记这次死在水域 + 清零饼干 + 复位涨水 + 传送到 waterArea.respawnPoint
            area.HandleDeathInsideExternal();

            // 只做演出（SFX + 死亡UI），不调用 pc.Die()，以免触发全局 checkpoint 再干预
            GlobalSfx.PlayDeathSfx();
            if (DeathUIOverlay.Instance)
            {
                if (holdSeconds > 0f)
                    DeathUIOverlay.Instance.Show(overrideMessage, overrideSprite, holdSeconds);
                else
                    DeathUIOverlay.Instance.Show(overrideMessage, overrideSprite, null);
            }

            StartCoroutine(ClearBusyNextFrame());
            return;
        }

        // —— Strategy B：没有/不使用水域处理时，若指定了“本区专属重生点”，覆盖下一次重生 —— //
        if (respawnOverride)
        {
            // 1) 若有 RespawnManager：建议使用“下一次复活点覆盖”接口（你项目若无，此步会被忽略）
            if (respawnManager)
            {
                respawnManager.OverrideNextRespawn(respawnOverride.position, respawnOverride.rotation);
            }
            else
            {
                // 2) 没有管理器也没关系：退而求其次，直接把玩家先搬到重生点，再调用 Die()
                //    这样 Die() 的全局流程即使再传送，也基本会落在同一位置，避免被传到“后面关卡”。
                SafeWarpImmediate(root, respawnOverride);
            }
        }

        // —— 真正执行通用“死亡”流程（交给全局生命周期处理）—— //
        pc.Die();
        GlobalSfx.PlayDeathSfx();

        if (DeathUIOverlay.Instance)
        {
            if (holdSeconds > 0f)
                DeathUIOverlay.Instance.Show(overrideMessage, overrideSprite, holdSeconds);
            else
                DeathUIOverlay.Instance.Show(overrideMessage, overrideSprite, null);
        }

        // —— 若开启水域集成但未采用 Strategy A，也至少把“这轮死过”记到水域里（不负责传送）—— //
        if (enableWaterAreaIntegration && area != null && area.IsPlayerInsideZonePublic())
        {
            area.NotifyPlayerDiedInside();
        }

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

    // 安全瞬移（处理 RB / CC / Agent），用于 Strategy B 的“先搬后死”兜底
    void SafeWarpImmediate(Transform t, Transform target)
    {
        if (!t || !target) return;

        var rb = t.GetComponent<Rigidbody>();
        var cc = t.GetComponent<CharacterController>();
        var agent = t.GetComponent<UnityEngine.AI.NavMeshAgent>();

        bool ccWas = false, agentWas = false;
        if (cc) { ccWas = cc.enabled; cc.enabled = false; }
        if (agent){ agentWas = agent.enabled; agent.enabled = false; }

        if (rb)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        t.SetPositionAndRotation(target.position, target.rotation);

        if (rb)
        {
            rb.isKinematic = false;
            rb.WakeUp();
        }
        if (cc) cc.enabled = ccWas;
        if (agent) agent.enabled = agentWas;

        Physics.SyncTransforms();
    }
}

// —— 只是一个示例接口；若你项目已有不同名字/签名的管理器，请在 Inspector 里拖入并适配 —— //
public class RespawnManager : MonoBehaviour
{
    // 供 SpiderDeathZone 可选调用的“下一次重生点覆盖”接口
    public void OverrideNextRespawn(Vector3 pos, Quaternion rot)
    {
        // TODO: 实现你项目里的“下次重生点”设置（例如存到一个静态字段，复活后清空）
    }
}
