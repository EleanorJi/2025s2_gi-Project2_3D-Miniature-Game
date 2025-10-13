// BossHealthBarUI.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[DisallowMultipleComponent]
public class BossHealthBarUI : MonoBehaviour
{
    [Header("References")]

    public Health bossHealth;


    public Slider slider;


    public Image fill;

    [Header("Visual")]

    public Color fillColor = new Color(0.83f, 0.18f, 0.18f, 1f); // #D32F2F

    [Tooltip("Whether to hide the health bar when the Boss dies")]
    public bool hideOnDeath = true;

    [Header("Auto Find")]

    public string bossTag = "Boss";


    public bool autoFindBoss = true;


    public float findRetryInterval = 0.5f;

    [Header("Game Over")]

    public GameObject gameOverCanvas;

    public float fadeInDuration = 1f;

    Coroutine _retryCo;

    void Awake()
    {
        if (!slider) slider = GetComponent<Slider>();
        if (!fill && slider && slider.fillRect)
            fill = slider.fillRect.GetComponent<Image>();
    }

    void Start()
    {
        // Initialize the UI appearance (without affecting the values)
        if (fill) fill.color = fillColor;

        // Try to bind immediately
        TryBind(bossHealth);

        // If not bound, try to find automatically
        if (!bossHealth && autoFindBoss)
            _retryCo = StartCoroutine(RetryBindRoutine());
        gameOverCanvas.SetActive(false);
    }

    void OnDisable()
    {
        Unsubscribe();
        if (_retryCo != null) { StopCoroutine(_retryCo); _retryCo = null; }
    }

    // —— binding ————————————————————————————————————————

    void TryBind(Health h)
    {
        if (!h)
        {
            var go = GameObject.FindGameObjectWithTag(bossTag);
            if (go) h = go.GetComponentInChildren<Health>();
        }
        if (!h) return;

        // Successfully found: Binding
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
        // Prevent duplicate binding
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

    // —— UI update ————————————————————————————————————————————————

    void InitSliderValues()
    {
        if (!slider) return;

        int max = bossHealth ? bossHealth.maxHealth : 100;
        int cur = bossHealth ? bossHealth.currentHealth : max;

        slider.minValue = 0;
        slider.maxValue = max;
        slider.value   = cur;

        // Set the fill color
        if (fill) fill.color = fillColor;
    }

    void OnHealthChanged(int current, int max)
    {
        if (!slider) return;
        slider.maxValue = max;
        slider.value = Mathf.Clamp(current, 0, max);
    }

    void OnBossDeath()
    {
        // Show game over screen
        if (gameOverCanvas)
        {
            gameOverCanvas.transform.SetAsLastSibling();

            var gameOverCg = gameOverCanvas.GetComponent<CanvasGroup>();
            if (gameOverCg == null)
            {
                gameOverCg = gameOverCanvas.AddComponent<CanvasGroup>();
            }

            gameOverCg.alpha = 0f;
            gameOverCanvas.SetActive(true);

            StartCoroutine(FadeIn(gameOverCg));
        }

        // Temporarily comment out the health bar hiding logic for testing
        // if (!hideOnDeath) return;
        // var healthBarCg = GetComponent<CanvasGroup>();
        // if (healthBarCg) StartCoroutine(FadeOut(healthBarCg));
        // else gameObject.SetActive(false);
    }




    IEnumerator FadeIn(CanvasGroup cg)
    {


        cg.alpha = 0f;
        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Clamp01(t / fadeInDuration);
            yield return null;
        }
        cg.alpha = 1f;

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
        // Editor-time auto-assign references
        if (!slider) slider = GetComponent<Slider>();
        if (!fill && slider && slider.fillRect)
            fill = slider.fillRect.GetComponent<Image>();
        if (fill) fill.color = fillColor;
    }
#endif
}
