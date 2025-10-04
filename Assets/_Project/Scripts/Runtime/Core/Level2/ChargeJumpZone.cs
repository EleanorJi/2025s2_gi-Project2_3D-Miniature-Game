using UnityEngine;

/// <summary>
/// 把本脚本挂到一个带 Box/Sphere/Capsule Collider 的触发区域（IsTrigger ✔）
/// 作用：玩家进入→开启 ChargeJumpModule；离开→关闭 ChargeJumpModule
/// 只依赖 ChargeJumpModule，不会访问 PlayerController。
/// </summary>
[RequireComponent(typeof(Collider))]
public class ChargeJumpZone : MonoBehaviour
{
    [Header("过滤")]
    public bool onlyAffectTag = true;
    public string requiredTag = "Player";   // 只影响带这个Tag的对象

    [Header("进入/离开行为")]
    public bool enableOnEnter = true;       // 进入时设为 true
    public bool disableOnExit = true;       // 离开时设为 false

    [Header("可选：离开后延迟关闭（秒）")]
    public float disableDelay = 0f;         // 0 = 立即关闭

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (onlyAffectTag && !other.CompareTag(requiredTag)) return;

        var mod = other.GetComponent<ChargeJumpModule>();
        if (!mod) return;

        if (enableOnEnter)
            mod.SetChargeEnabled(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (onlyAffectTag && !other.CompareTag(requiredTag)) return;

        var mod = other.GetComponent<ChargeJumpModule>();
        if (!mod) return;

        if (disableOnExit)
        {
            if (disableDelay <= 0f)
            {
                mod.SetChargeEnabled(false);
            }
            else
            {
                StartCoroutine(DisableLater(mod, disableDelay));
            }
        }
    }

    System.Collections.IEnumerator DisableLater(ChargeJumpModule mod, float t)
    {
        yield return new WaitForSeconds(t);
        // 目标可能已被销毁/离开场景
        if (mod) mod.SetChargeEnabled(false);
    }

#if UNITY_EDITOR
    // 小可视化
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
        Gizmos.matrix = transform.localToWorldMatrix;
        var box = GetComponent<BoxCollider>();
        if (box && box.isTrigger) Gizmos.DrawCube(box.center, box.size);
    }
#endif
}
