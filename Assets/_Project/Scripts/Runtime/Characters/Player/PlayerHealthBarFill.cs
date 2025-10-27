using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBarFill : MonoBehaviour
{
    [Header("Refs")]
    public PlayerHealth target;   // 留空自动找 Tag=Player
    public Image hpImg;           // 绿色条
    public Image hpEffectImg;     // 缓冲条

    [Header("Timings")]
    public float damageLagTime = 0.35f;  // 掉血时缓冲条延迟跟随
    public float healRiseTime  = 0.20f;  // 回血时绿色条上升动画

    [Header("Visual Calibration (optional)")]
    public float minFill = 0f;
    public float maxFill = 1f;

    Coroutine _anim;
    float _currentPct = 1f;

    void Awake()
    {
        EnsureTarget();
        ForceFilled(hpImg);
        ForceFilled(hpEffectImg);

        // 初始同步（避免第一次受击前显示不对）
        ImmediateRefreshFromTarget();
    }

    void OnEnable()
    {
        EnsureTarget();
        Subscribe(true);

        // 关键：对象重新启用时，可能错过了重生事件 → 主动刷新
        ImmediateRefreshFromTarget();
    }

    void OnDisable()
    {
        if (_anim != null) { StopCoroutine(_anim); _anim = null; }
        Subscribe(false);
    }

    // —— 事件 —— //
    void OnRespawned()
    {
        // 有些系统在重生流程里会先恢复数值再开启UI，这里再等1帧更稳妥
        StartCoroutine(NextFrameRefresh());
    }

    IEnumerator NextFrameRefresh()
    {
        yield return null; // 等一帧，确保血量已恢复 & 位置稳定
        ImmediateRefreshFromTarget();
    }

    void OnPctChanged(float pct)
    {
        pct = Mathf.Clamp01(pct);
        if (pct < _currentPct)
        {
            // 掉血：绿色条瞬到，缓冲条延后跟随
            SetGreen(pct);
            StartAnim(LerpEffect(hpEffectImg ? hpEffectImg.fillAmount : _currentPct, pct, damageLagTime));
        }
        else if (pct > _currentPct)
        {
            // 回血：先提缓冲条到目标，再把绿色条平滑抬上去
            SetEffect(pct);
            StartAnim(LerpGreen(_currentPct, pct, healRiseTime));
        }
    }

    // —— 工具 —— //
    void EnsureTarget()
    {
        if (target) return;
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) target = p.GetComponent<PlayerHealth>();
        if (!target) target = FindObjectOfType<PlayerHealth>(true);
    }

    void Subscribe(bool on)
    {
        if (!target) return;
        if (on)
        {
            target.OnHealthPctChanged.AddListener(OnPctChanged);
            target.OnRespawned.AddListener(OnRespawned);
        }
        else
        {
            target.OnHealthPctChanged.RemoveListener(OnPctChanged);
            target.OnRespawned.RemoveListener(OnRespawned);
        }
    }

    void ForceFilled(Image img)
    {
        if (!img) return;
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillOrigin = (int)Image.OriginHorizontal.Left;
    }

    float Map01(float pct) => Mathf.Lerp(minFill, maxFill, Mathf.Clamp01(pct));

    void ImmediateRefreshFromTarget()
    {
        float pct = 1f;
        if (target && target.maxHealth > 0)
            pct = (float)target.CurrentHealth / target.maxHealth;

        SetImmediate(pct);
    }

    void SetImmediate(float pct)
    {
        if (_anim != null) { StopCoroutine(_anim); _anim = null; }
        SetGreen(pct);
        SetEffect(pct);
    }

    void SetGreen(float pct)
    {
        _currentPct = Mathf.Clamp01(pct);
        if (hpImg) hpImg.fillAmount = Map01(_currentPct);
    }

    void SetEffect(float pct)
    {
        if (hpEffectImg) hpEffectImg.fillAmount = Map01(Mathf.Clamp01(pct));
    }

    void StartAnim(IEnumerator co)
    {
        if (_anim != null) StopCoroutine(_anim);
        _anim = StartCoroutine(co);
    }

    IEnumerator LerpEffect(float fromRaw, float toPct, float time)
    {
        if (!hpEffectImg) yield break;
        float from = fromRaw;
        float to   = Map01(toPct);
        if (time <= 0f) { hpEffectImg.fillAmount = to; yield break; }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / time;
            hpEffectImg.fillAmount = Mathf.Lerp(from, to, t);
            yield return null;
        }
        hpEffectImg.fillAmount = to;
        _anim = null;
    }

    IEnumerator LerpGreen(float fromPct, float toPct, float time)
    {
        if (!hpImg) yield break;
        float from = Map01(fromPct);
        float to   = Map01(toPct);
        if (time <= 0f) { hpImg.fillAmount = to; SetGreen(toPct); yield break; }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / time;
            hpImg.fillAmount = Mathf.Lerp(from, to, t);
            yield return null;
        }
        SetGreen(toPct);
        _anim = null;
    }

    // 如果想在别的脚本里手动强制刷新，可以公开一个方法：
    public void RefreshNow() => ImmediateRefreshFromTarget();
}
