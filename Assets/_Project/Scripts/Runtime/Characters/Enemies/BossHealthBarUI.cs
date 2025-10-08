// BossHealthBarUI.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[DisallowMultipleComponent]
public class BossHealthBarUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Boss 的生命组件。可留空，脚本会按 Tag 查找并绑定")]
    public Health bossHealth;

    [Tooltip("血条 Slider（建议把脚本挂在 Slider 上，没填会自动 GetComponent）")]
    public Slider slider;

    [Tooltip("Slider 的 Fill 图像（不填会尝试 slider.fillRect → GetComponent<Image>() 自动获取）")]
    public Image fill;

    [Header("Visual")]
    [Tooltip("Fill 的固有颜色（默认红色）。脚本不会在运行时改它，避免变白问题")]
    public Color fillColor = new Color(0.83f, 0.18f, 0.18f, 1f); // #D32F2F

    [Tooltip("血条是否在 Boss 死亡时隐藏")]
    public bool hideOnDeath = true;

    [Header("Auto Find")]
    [Tooltip("用于自动查找 Boss 的 Tag")]
    public string bossTag = "Boss";

    [Tooltip("启动时未找到 Boss 时，是否定时重试绑定")]
    public bool autoFindBoss = true;

    [Tooltip("自动重试绑定的间隔秒")]
    public float findRetryInterval = 0.5f;

    Coroutine _retryCo;

    void Awake()
    {
        if (!slider) slider = GetComponent<Slider>();
        if (!fill && slider && slider.fillRect)
            fill = slider.fillRect.GetComponent<Image>();
    }

    void Start()
    {
        // 初始化 UI 外观（不影响数值）
        if (fill) fill.color = fillColor;

        // 先尝试立即绑定
        TryBind(bossHealth);

        // 绑定不到就自动找
        if (!bossHealth && autoFindBoss)
            _retryCo = StartCoroutine(RetryBindRoutine());
    }

    void OnDisable()
    {
        Unsubscribe();
        if (_retryCo != null) { StopCoroutine(_retryCo); _retryCo = null; }
    }

    // —— 绑定 & 事件 ————————————————————————————————————————

    void TryBind(Health h)
    {
        if (!h)
        {
            var go = GameObject.FindGameObjectWithTag(bossTag);
            if (go) h = go.GetComponentInChildren<Health>();
        }
        if (!h) return;

        // 成功找到：绑定
        bossHealth = h;
        Subscribe();
        InitSliderValues();
    }

    IEnumerator RetryBindRoutine()
    {
        while (!bossHealth)
        {
            TryBind(null);
            if (!bossHealth) yield return new WaitForSeconds(findRetryInterval);
        }
        _retryCo = null;
    }

    void Subscribe()
    {
        if (!bossHealth) return;
        // 先防止重复绑
        Unsubscribe();
        bossHealth.onHealthChanged.AddListener(OnHealthChanged);
        bossHealth.onDeath.AddListener(OnBossDeath);
    }

    void Unsubscribe()
    {
        if (!bossHealth) return;
        bossHealth.onHealthChanged.RemoveListener(OnHealthChanged);
        bossHealth.onDeath.RemoveListener(OnBossDeath);
    }

    // —— UI 更新 ————————————————————————————————————————————————

    void InitSliderValues()
    {
        if (!slider) return;

        int max = bossHealth ? bossHealth.maxHealth : 100;
        int cur = bossHealth ? bossHealth.currentHealth : max;

        slider.minValue = 0;
        slider.maxValue = max;
        slider.value   = cur;

        // 只设置一次颜色，避免“开局变白”
        if (fill) fill.color = fillColor;
    }

    void OnHealthChanged(int current, int max)
    {
        if (!slider) return;
        slider.maxValue = max;
        slider.value = Mathf.Clamp(current, 0, max);
        // 不在这里改 fill.color，避免颜色被覆盖成默认白
    }

    void OnBossDeath()
    {
        if (!hideOnDeath) return;

        // 有 CanvasGroup 就淡出；没有就直接隐藏
        var cg = GetComponent<CanvasGroup>();
        if (cg) StartCoroutine(FadeOut(cg));
        else gameObject.SetActive(false);
    }

    IEnumerator FadeOut(CanvasGroup cg)
    {
        float t = 0f;
        while (t < 0.25f)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = 1f - Mathf.Clamp01(t / 0.25f);
            yield return null;
        }
        gameObject.SetActive(false);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // 在编辑器中也尽量自动补引用
        if (!slider) slider = GetComponent<Slider>();
        if (!fill && slider && slider.fillRect)
            fill = slider.fillRect.GetComponent<Image>();
        if (fill) fill.color = fillColor;
    }
#endif
}
