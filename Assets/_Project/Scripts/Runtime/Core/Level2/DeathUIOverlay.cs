using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DeathUIOverlay : MonoBehaviour
{
    public static DeathUIOverlay Instance { get; private set; }

    [Header("UI bond")]
    public CanvasGroup panel;
    public TMP_Text    messageText;
    public Image       pictureImage;

    [Header("Default Context")]
    [TextArea]
    public string defaultMessage = "You dead...";
    public Sprite defaultSprite;

    [Header("Default Time")]
    public float defaultHoldSeconds = 4f;

    Coroutine _co;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (panel) { panel.alpha = 0f; panel.blocksRaycasts = false; }
    }

    
    public void ShowForSeconds(float holdSeconds)
    {
        ShowInternal(holdSeconds, null, null);
    }

    // New Interface 1: Supports custom text/images/duration  //
    public void Show(string overrideMessage = null, Sprite overrideSprite = null, float? holdSeconds = null)
    {
        ShowInternal(holdSeconds ?? defaultHoldSeconds, overrideMessage, overrideSprite);
    }

    // New Interface 2: Only covers text and images; duration uses default settings. //
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
        // text message
        if (messageText)
            messageText.text = string.IsNullOrEmpty(msgOverride) ? defaultMessage : msgOverride;

        if (pictureImage)
            pictureImage.sprite = spriteOverride ? spriteOverride : defaultSprite;

        // fade in
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

        // stay
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, hold));

        // fade out
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
