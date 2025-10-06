using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// 挂到一个带 Box/Sphere/Capsule Collider 的触发区域（IsTrigger ✔）
/// 作用：玩家进入→开启 ChargeJumpModule；离开→关闭 ChargeJumpModule
/// 现在新增：进入区域时弹出一次提示UI（可选，不填引用就不显示）
/// </summary>
[RequireComponent(typeof(Collider))]
public class ChargeJumpZone : MonoBehaviour
{
    [Header("过滤")]
    public bool onlyAffectTag = true;
    public string requiredTag = "Player";   // 只影响这个 Tag

    [Header("进入/离开行为")]
    public bool enableOnEnter = true;       // 进入：开启蓄力跳
    public bool disableOnExit = true;       // 离开：关闭蓄力跳
    [Tooltip("离开后延迟关闭（秒），0=立即关闭")]
    public float disableDelay = 0f;

    [Header("进入提示UI（可选）")]
    public CanvasGroup hintGroup;           // 拖一个面板（CanvasGroup），默认 alpha=0
    public TMP_Text hintText;               // 面板上的文本
    [TextArea] public string enterMessage = "在这个区域不要被粘稠的垃圾汤粘住！";
    public float hintFadeIn = 0.2f;
    public float hintStay   = 2.0f;
    public float hintFadeOut= 0.2f;
    public bool  hideOnExit = true;         // 离开时立刻把提示隐藏（可选）

    Coroutine hintCo;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (onlyAffectTag && !other.CompareTag(requiredTag)) return;

        // 开启蓄力跳
        var mod = other.GetComponent<ChargeJumpModule>();
        if (mod && enableOnEnter) mod.SetChargeEnabled(true);

        // 弹提示（可选）
        if (hintGroup && hintText)
        {
            ShowHintOnce(enterMessage);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (onlyAffectTag && !other.CompareTag(requiredTag)) return;

        // 关闭蓄力跳
        var mod = other.GetComponent<ChargeJumpModule>();
        if (mod && disableOnExit)
        {
            if (disableDelay <= 0f) mod.SetChargeEnabled(false);
            else StartCoroutine(DisableLater(mod, disableDelay));
        }

        // 离开时可选隐藏提示
        if (hideOnExit) HideHintImmediate();
    }

    IEnumerator DisableLater(ChargeJumpModule mod, float t)
    {
        yield return new WaitForSeconds(t);
        if (mod) mod.SetChargeEnabled(false);
    }

    // —— 提示UI —— //
    void ShowHintOnce(string msg)
    {
        if (!hintGroup || !hintText) return;
        if (hintCo != null) StopCoroutine(hintCo);
        hintCo = StartCoroutine(HintRoutine(msg));
    }

    IEnumerator HintRoutine(string msg)
    {
        hintText.text = msg;

        // 淡入
        for (float t = 0f; t < hintFadeIn; t += Time.unscaledDeltaTime)
        {
            hintGroup.alpha = Mathf.Lerp(0f, 1f, t / hintFadeIn);
            yield return null;
        }
        hintGroup.alpha = 1f;

        // 停留
        yield return new WaitForSecondsRealtime(hintStay);

        // 淡出
        for (float t = 0f; t < hintFadeOut; t += Time.unscaledDeltaTime)
        {
            hintGroup.alpha = Mathf.Lerp(1f, 0f, t / hintFadeOut);
            yield return null;
        }
        hintGroup.alpha = 0f;
        hintCo = null;
    }

    void HideHintImmediate()
    {
        if (!hintGroup) return;
        if (hintCo != null) StopCoroutine(hintCo);
        hintGroup.alpha = 0f;
        hintCo = null;
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
