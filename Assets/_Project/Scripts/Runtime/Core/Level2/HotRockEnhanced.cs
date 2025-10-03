using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class HotRockEnhanced : MonoBehaviour
{
    [Header("规则")]
    public float maxStandSeconds = 5f;     // 最多可停留
    public float warnRemainSeconds = 3f;   // 剩余3秒开始警告
    public string playerTag = "Player";

    [Header("UI（全屏红闪）")]
    public CanvasGroup hotOverlay;         // ← 拖 HotOverlay 的 CanvasGroup
    public TMP_Text hotText;               // ← 拖 HotText
    public float flashFreq = 6f;           // 闪烁频率（次/秒）

    // 记录每个玩家在这块岩石上的进入时间
    private Dictionary<int, float> enterTime = new();

    void OnCollisionEnter(Collision c)
    {
        if (!c.collider.CompareTag(playerTag)) return;
        int id = c.collider.GetInstanceID();
        enterTime[id] = Time.time;
        HideWarn(); // 重置 UI
    }

    void OnCollisionStay(Collision c)
    {
        if (!c.collider.CompareTag(playerTag)) return;
        int id = c.collider.GetInstanceID();
        if (!enterTime.ContainsKey(id)) enterTime[id] = Time.time;

        float stayed = Time.time - enterTime[id];
        float remain = maxStandSeconds - stayed;

        // 3秒内开始红色闪烁提示
        if (remain <= warnRemainSeconds && remain > 0f)
            ShowWarn(remain);
        else
            HideWarn();

        // 超时死亡
        if (stayed >= maxStandSeconds)
        {
            var pc = c.collider.GetComponent<PlayerController>();
            if (pc) pc.Die();
            HideWarn();
        }
    }

    void OnCollisionExit(Collision c)
    {
        if (!c.collider.CompareTag(playerTag)) return;
        int id = c.collider.GetInstanceID();
        if (enterTime.ContainsKey(id)) enterTime.Remove(id);
        HideWarn();

        // 记录“上一次起跳的岩石”
        LastJumpRock.Set(transform);
    }

    void ShowWarn(float remain)
    {
        if (!hotOverlay) return;
        // 闪烁：alpha 在 0～0.35 之间
        float a = 0.35f * (0.5f + 0.5f * Mathf.Sin(Time.time * flashFreq * Mathf.PI * 2f));
        hotOverlay.alpha = a;
        if (hotText) hotText.text = "So hot! So hot!";
    }

    void HideWarn()
    {
        if (hotOverlay) hotOverlay.alpha = 0f;
    }
}

/// 全局记录最后一次起跳的岩石
public static class LastJumpRock
{
    public static Transform last;
    public static void Set(Transform t) { last = t; }
}
