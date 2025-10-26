using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBarFill : MonoBehaviour
{
    public PlayerHealth target;   // 可留空：自动用 Tag=Player
    public Image hpImg;           // 前景条（绿色）  Image Type=Filled
    public Image hpEffectImg;     // 缓冲条（黄色/红色） Image Type=Filled

    [Header("Timings")]
    public float damageLagTime = 0.35f;  // 掉血时缓冲条延迟追随
    public float healRiseTime  = 0.20f;  // 回血时前景条上升时间

    private Coroutine _co;

    private void Awake()
    {
        if (!target)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) target = p.GetComponent<PlayerHealth>();
            if (!target) target = FindObjectOfType<PlayerHealth>();
        }

        // 初始化
        float pct = (target && target.maxHealth > 0)
            ? (float)target.CurrentHealth / target.maxHealth
            : 0f;

        if (hpImg)       hpImg.fillAmount       = pct;
        if (hpEffectImg) hpEffectImg.fillAmount = pct;

        if (target) target.OnHealthPctChanged.AddListener(HandlePct);
    }

    private void OnDisable()
    {
        if (target) target.OnHealthPctChanged.RemoveListener(HandlePct);
        if (_co != null) StopCoroutine(_co);
        _co = null;
    }

    private void HandlePct(float pct)
    {
        if (!hpImg || !hpEffectImg) return;
        pct = Mathf.Clamp01(pct);

        // 掉血：前景条瞬间到位，缓冲条慢慢落下
        if (hpImg.fillAmount > pct)
        {
            hpImg.fillAmount = pct;
            if (_co != null) StopCoroutine(_co);
            _co = StartCoroutine(LerpFill(hpEffectImg, hpEffectImg.fillAmount, pct, damageLagTime));
        }
        // 回血：缓冲条先跳到目标，前景条再平滑上升
        else
        {
            hpEffectImg.fillAmount = pct;
            if (_co != null) StopCoroutine(_co);
            _co = StartCoroutine(LerpFill(hpImg, hpImg.fillAmount, pct, healRiseTime));
        }
    }

    private static IEnumerator LerpFill(Image img, float from, float to, float time)
    {
        if (time <= 0f) { img.fillAmount = to; yield break; }
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            img.fillAmount = Mathf.Lerp(from, to, t / time);
            yield return null;
        }
        img.fillAmount = to;
    }
}
