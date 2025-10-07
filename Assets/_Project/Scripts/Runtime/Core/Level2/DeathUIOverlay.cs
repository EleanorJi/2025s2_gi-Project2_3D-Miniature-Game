using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DeathUIOverlay : MonoBehaviour
{
    public static DeathUIOverlay Instance { get; private set; }

    [Header("UI 绑定")]
    public CanvasGroup panel;         // 整个死亡UI的CanvasGroup
    public TMP_Text    messageText;   // 文案（可留空）
    public Image       pictureImage;  // 图片（可留空）

    [Header("默认内容")]
    [TextArea]
    public string defaultMessage = "你死了";
    public Sprite defaultSprite;

    [Header("默认时长")]
    public float defaultHoldSeconds = 4f;

    Coroutine _co;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (panel) { panel.alpha = 0f; panel.blocksRaycasts = false; }
    }

    // —— 兼容旧接口：只给时长 —— //
    public void ShowForSeconds(float holdSeconds)
    {
        ShowInternal(holdSeconds, null, null);
    }

    // —— 新接口1：可传自定义文案/图片/时长（任意可空）—— //
    public void Show(string overrideMessage = null, Sprite overrideSprite = null, float? holdSeconds = null)
    {
        ShowInternal(holdSeconds ?? defaultHoldSeconds, overrideMessage, overrideSprite);
    }

    // —— 新接口2：只覆盖文案和图片，时长用默认 —— //
    public void ShowWithOverrides(string overrideMessage = null, Sprite overrideSprite = null)
    {
        ShowInternal(defaultHoldSeconds, overrideMessage, overrideSprite);
    }

    void ShowInternal(float hold, string msgOverride, Sprite spriteOverride)
    {
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(Run(hold, msgOverride, spriteOverride));
    }

    IEnumerator Run(float hold, string msgOverride, Sprite spriteOverride)
    {
        // 内容赋值（留空就用默认）
        if (messageText)
            messageText.text = string.IsNullOrEmpty(msgOverride) ? defaultMessage : msgOverride;

        if (pictureImage)
            pictureImage.sprite = spriteOverride ? spriteOverride : defaultSprite;

        // 淡入
        if (panel)
        {
            panel.blocksRaycasts = true;
            for (float t = 0; t < 0.2f; t += Time.unscaledDeltaTime)
            {
                panel.alpha = Mathf.Lerp(0f, 1f, t / 0.2f);
                yield return null;
            }
            panel.alpha = 1f;
        }

        // 停留
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, hold));

        // 淡出
        if (panel)
        {
            for (float t = 0; t < 0.2f; t += Time.unscaledDeltaTime)
            {
                panel.alpha = Mathf.Lerp(1f, 0f, t / 0.2f);
                yield return null;
            }
            panel.alpha = 0f;
            panel.blocksRaycasts = false;
        }
        _co = null;
    }
}
