using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CheckpointOneWayWall : MonoBehaviour
{
    [Header("Who to lock")]
    public string playerTag = "Player";

    [Header("The air wall to enable")]
    [Tooltip("两者选其一：直接启用这个 Collider…")]
    public Collider wallCollider;
    [Tooltip("…或者激活这整个墙体 GameObject")]
    public GameObject wallObject;

    [Header("Behavior")]
    [Tooltip("只在本轮（本次运行）触发一次")]
    public bool oncePerSession = true;
    [Tooltip("本触发器的全局唯一ID（空则自动用层级路径）")]
    public string checkpointId = "";

    [Tooltip("启用墙体前，临时忽略玩家与墙体的碰撞，避免把玩家夹进墙里")]
    public float ignoreCollisionSeconds = 0.2f;

    // === 前进方向设置 ===
    public enum DirectionMode { UseTransformForward, UseAxis, UseCustomVector }
    [Header("Forward Direction")]
    public DirectionMode directionMode = DirectionMode.UseTransformForward;

    [Tooltip("当选择 UseAxis 时生效")]
    public Axis axis = Axis.Z;
    public enum Axis { X, Y, Z }

    [Tooltip("轴向为正(+1)还是负(-1)")]
    public bool positive = true;

    [Tooltip("当选择 UseCustomVector 时生效（世界坐标系）。例如 (1,0,0) 表示世界+X 为前进方向。")]
    public Vector3 customWorldDirection = Vector3.forward;

    [Tooltip("整体反向（相当于把前进/后退对调）")]
    public bool invertDirection = false;

    // 会话内记忆
    static HashSet<string> activatedIds = new HashSet<string>();

    // 内部
    bool _armed = true;     // 允许触发
    int _lastSide = 0;      // 进入/离开时记录玩家在哪一侧（-1/ +1）

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
        if (string.IsNullOrEmpty(checkpointId))
            checkpointId = BuildAutoId();
    }

    void Awake()
    {
        if (string.IsNullOrEmpty(checkpointId))
            checkpointId = BuildAutoId();

        // 如果本轮已经触发过，就直接把墙体启用（按需可改为保持关闭）
        if (oncePerSession && activatedIds.Contains(checkpointId))
            ActivateWallImmediate();
        else
            DeactivateWallImmediate(); // 确保初始是关闭的
    }

    void OnTriggerEnter(Collider other)
    {
        if (!_armed || !other.CompareTag(playerTag)) return;
        _lastSide = SideOf(other.transform.position);
    }

    void OnTriggerExit(Collider other)
    {
        if (!_armed || !other.CompareTag(playerTag)) return;
        int nowSide = SideOf(other.transform.position);

        // 只在“从后侧(-1) → 前侧(+1)”穿过时触发上锁
        if (_lastSide < 0 && nowSide > 0)
        {
            LockBackPath(other);
        }
    }

    // 计算玩家相对“存档线”的侧：>0 表示在 forward 方向一侧，<0 在后侧
    int SideOf(Vector3 worldPos)
    {
        Vector3 fwd = GetForward();
        Vector3 toPoint = worldPos - transform.position;
        float dot = Vector3.Dot(fwd, toPoint);
        return dot >= 0f ? +1 : -1;
    }

    Vector3 GetForward()
    {
        Vector3 fwd;
        switch (directionMode)
        {
            case DirectionMode.UseAxis:
                switch (axis)
                {
                    case Axis.X: fwd = positive ? Vector3.right  : Vector3.left;  break;
                    case Axis.Y: fwd = positive ? Vector3.up     : Vector3.down;  break;
                    default:     fwd = positive ? Vector3.forward: Vector3.back;  break;
                }
                break;

            case DirectionMode.UseCustomVector:
                fwd = customWorldDirection;
                if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.forward; // 兜底
                break;

            default:
                fwd = transform.forward;
                break;
        }
        if (invertDirection) fwd = -fwd;
        return fwd.normalized;
    }

    void LockBackPath(Collider player)
    {
        if (oncePerSession) activatedIds.Add(checkpointId);

        ActivateWallImmediate();

        // 短暂忽略玩家与墙体的碰撞，避免立刻被卡住
        if (wallCollider)
        {
            var playerCol = player; // 如需精确可抓玩家的 CapsuleCollider/CharacterController
            Physics.IgnoreCollision(playerCol, wallCollider, true);
            StartCoroutine(ReenableCollisionLater(playerCol));
        }

        _armed = false; // 不再重复触发
    }

    IEnumerator ReenableCollisionLater(Collider playerCol)
    {
        float t = 0f;
        while (t < ignoreCollisionSeconds)
        {
            t += Time.unscaledDeltaTime; // 暂停也计时
            yield return null;
        }
        if (wallCollider && playerCol)
            Physics.IgnoreCollision(playerCol, wallCollider, false);
    }

    void ActivateWallImmediate()
    {
        if (wallObject)   wallObject.SetActive(true);
        if (wallCollider) wallCollider.enabled = true;
    }

    void DeactivateWallImmediate()
    {
        if (wallObject)   wallObject.SetActive(false);
        if (wallCollider) wallCollider.enabled = false;
    }

    string BuildAutoId()
    {
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

    // —— 方向可视化 —— //
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 fwd = Application.isPlaying ? GetForward() : PreviewForwardInEditor();
        Vector3 p = transform.position;
        Gizmos.DrawLine(p, p + fwd * 2.0f);
        DrawArrowHead(p + fwd * 2.0f, fwd);
    }

    Vector3 PreviewForwardInEditor()
    {
        // 运行前也能根据 inspector 预览方向
        Vector3 fwd;
        switch (directionMode)
        {
            case DirectionMode.UseAxis:
                switch (axis)
                {
                    case Axis.X: fwd = positive ? Vector3.right : Vector3.left; break;
                    case Axis.Y: fwd = positive ? Vector3.up    : Vector3.down; break;
                    default:     fwd = positive ? Vector3.forward:Vector3.back; break;
                }
                break;
            case DirectionMode.UseCustomVector:
                fwd = customWorldDirection.sqrMagnitude < 1e-6f ? Vector3.forward : customWorldDirection;
                break;
            default:
                fwd = transform.forward;
                break;
        }
        if (invertDirection) fwd = -fwd;
        return fwd.normalized;
    }

    void DrawArrowHead(Vector3 tip, Vector3 dir)
    {
        Vector3 right = Quaternion.AngleAxis(25f, Vector3.up) * (-dir);
        Vector3 left  = Quaternion.AngleAxis(-25f, Vector3.up) * (-dir);
        Gizmos.DrawLine(tip, tip + right * 0.4f);
        Gizmos.DrawLine(tip, tip + left  * 0.4f);
    }
}
