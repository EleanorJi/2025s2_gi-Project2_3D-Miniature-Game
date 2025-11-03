using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class NoCookieUI : MonoBehaviour
{
    public static NoCookieUI Instance;

    [Header("Assets")]
    public Sprite cookieIcon;                // cookieicon
    public Sprite roundedPanelSprite;        // Optional 9-sliced
    public TMP_FontAsset textFont;           // Optional

    [Header("Style")]
    [Range(0f, 1f)] public float panelAlpha = 0.60f;
    public Vector2 panelSize = new Vector2(520, 168);
    public float cornerPadding = 16f;

    [Header("Animation")]
    public float popInTime = 0.12f;
    public float holdTime = 0.0f;
    public float fadeOutTime = 1.0f;
    public Vector3 popFromScale = new Vector3(0.9f, 0.9f, 1f);

    [Header("Text")]
    [TextArea] public string defaultMessage = "You need at least 1 cookie to summon a minion.";

    public CanvasGroup fadePanel;
    public float fadeDuration = 1f;
   
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        StartCoroutine(CoFadeToBlackAndLoadNext());
    }

    public static void ShowCenter(string message = null)
    {
        if (Instance == null) { Debug.LogWarning("[NoCookieUI] Add this script to a Canvas first."); return; }
        Instance.SpawnToast(message);
    }

    void SpawnToast(string message)
    {
        var parent = transform as RectTransform;

        var rootGO = new GameObject("NoCookieToast", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        var rt = rootGO.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = panelSize;

        var panelImg = rootGO.GetComponent<Image>();
        panelImg.color = new Color(1f, 1f, 1f, panelAlpha);
        panelImg.type = roundedPanelSprite ? Image.Type.Sliced : Image.Type.Simple;
        if (roundedPanelSprite) panelImg.sprite = roundedPanelSprite;

        var cg = rootGO.GetComponent<CanvasGroup>();
        cg.alpha = 0f;
        rootGO.transform.localScale = popFromScale;

        // icon
        if (cookieIcon)
        {
            var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var iconRT = iconGO.GetComponent<RectTransform>();
            iconRT.SetParent(rt, false);
            iconRT.anchorMin = iconRT.anchorMax = new Vector2(0f, 1f);
            iconRT.pivot = new Vector2(0f, 1f);
            iconRT.anchoredPosition = new Vector2(cornerPadding, -cornerPadding);
            iconRT.sizeDelta = new Vector2(48, 48);
            var iconImg = iconGO.GetComponent<Image>();
            iconImg.sprite = cookieIcon;
            iconImg.raycastTarget = false;
        }

        // title
        var titleGO = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        var titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.SetParent(rt, false);
        titleRT.anchorMin = titleRT.anchorMax = new Vector2(0.5f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0f, -cornerPadding - 2f);
        titleRT.sizeDelta = new Vector2(panelSize.x - 2 * cornerPadding, 36f);
        var titleTMP = titleGO.GetComponent<TextMeshProUGUI>();
        if (textFont) titleTMP.font = textFont;
        titleTMP.text = "Not enough cookies";
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.fontSize = 22f;
        titleTMP.color = new Color(0.18f, 0.18f, 0.18f, 0.95f);
        titleTMP.fontStyle = FontStyles.Bold;

        // body
        var textGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.SetParent(rt, false);
        textRT.anchorMin = new Vector2(0f, 0f);
        textRT.anchorMax = new Vector2(1f, 1f);
        textRT.pivot = new Vector2(0.5f, 0.5f);
        textRT.offsetMin = new Vector2(cornerPadding, cornerPadding);
        textRT.offsetMax = new Vector2(-cornerPadding, -cornerPadding - 12f);
        var tmp = textGO.GetComponent<TextMeshProUGUI>();
        if (textFont) tmp.font = textFont;
        tmp.text = string.IsNullOrEmpty(message) ? defaultMessage : message;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 26f;
        tmp.color = new Color(0.12f, 0.12f, 0.12f, 1f);

        StartCoroutine(PlayAndDestroy(rootGO, cg));
    }

    IEnumerator PlayAndDestroy(GameObject root, CanvasGroup cg)
    {
        float t = 0f;
        Vector3 startS = popFromScale;
        while (t < popInTime)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / popInTime);
            cg.alpha = Mathf.Lerp(0f, 1f, k);
            root.transform.localScale = Vector3.Lerp(startS, Vector3.one, k);
            yield return null;
        }
        if (holdTime > 0f)
            yield return new WaitForSecondsRealtime(holdTime);

        t = 0f;
        while (t < fadeOutTime)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fadeOutTime);
            cg.alpha = 1f - k;
            yield return null;
        }
        Destroy(root);
    }

    private IEnumerator CoFadeToBlackAndLoadNext()
    {

        

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            fadePanel.alpha = Mathf.Lerp(1f, 0f, t); // Fade from 1 to 0
            yield return null;
        }

        fadePanel.alpha = 0f; // Ensure final value is accurate

    }

}
