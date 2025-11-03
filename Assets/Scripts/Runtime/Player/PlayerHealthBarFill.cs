using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBarFill : MonoBehaviour
{
    [Header("Refs")]
    public PlayerHealth target;   // find Tag=Player
    public Image hpImg;           // green bar
    public Image hpEffectImg;     // buffer bar

    [Header("Timings")]
    public float damageLagTime = 0.35f;  // delay for buffer bar when taking damage
    public float healRiseTime  = 0.20f;  // animation time for green bar when healing

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

        // initial sync (avoid incorrect display before first hit)
        ImmediateRefreshFromTarget();
    }

    void OnEnable()
    {
        EnsureTarget();
        Subscribe(true);

        // key point: when the object is re-enabled, it may miss the respawn event → actively refresh
        ImmediateRefreshFromTarget();
    }

    void OnDisable()
    {
        if (_anim != null) { StopCoroutine(_anim); _anim = null; }
        Subscribe(false);
    }


    void OnRespawned()
    {
        // Some systems may restore values before opening the UI during the respawn process,
        // so it's safer to wait for 1 frame here.
        StartCoroutine(NextFrameRefresh());
    }

    IEnumerator NextFrameRefresh()
    {
        yield return null; // wait for 1 frame to ensure health is restored & position is stable
        ImmediateRefreshFromTarget();
    }

    void OnPctChanged(float pct)
    {
        pct = Mathf.Clamp01(pct);
        if (pct < _currentPct)
        {
            // Taking damage: green bar snaps to new value, buffer bar follows with delay
            SetGreen(pct);
            StartAnim(LerpEffect(hpEffectImg ? hpEffectImg.fillAmount : _currentPct, pct, damageLagTime));
        }
        else if (pct > _currentPct)
        {
            // Healing: buffer bar rises to target, then green bar smoothly follows
            SetEffect(pct);
            StartAnim(LerpGreen(_currentPct, pct, healRiseTime));
        }
    }


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


    public void RefreshNow() => ImmediateRefreshFromTarget();
}
