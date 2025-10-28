using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace Antventure.UI
{
    /// <summary>
    /// Global Death UI Controller - Unified management of UI display for all death scenarios
    /// Supports center-screen limited size death image display
    /// </summary>
    public class GlobalDeathUIController : MonoBehaviour
    {
        public static GlobalDeathUIController Instance { get; private set; }

        [Header("UI Components")]
        [SerializeField] private Canvas deathCanvas;
        [SerializeField] private CanvasGroup deathPanel;
        [SerializeField] private Image deathImage;
        [SerializeField] private TextMeshProUGUI deathMessageText;
        [SerializeField] private Image backgroundOverlay; // Semi-transparent background overlay

        [Header("Display Settings")]
        [SerializeField] [Range(0.1f, 1f)] private float imageMaxScreenRatio = 0.4f; // Maximum image screen ratio
        [SerializeField] [Range(0f, 1f)] private float backgroundAlpha = 0.7f; // Background transparency
        [SerializeField] private Vector2 imageMaxSize = new Vector2(600, 400); // Suggested maximum image size (only as limit when screen ratio calculation is larger)
        [SerializeField] private bool maintainAspectRatio = true; // Maintain aspect ratio
        [SerializeField] private bool useImageMaxSizeLimit = false; // Enable image maximum size limit

        [Header("Animation Settings")]
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float fadeOutDuration = 0.3f;
        [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Default Settings")]
        [SerializeField] private string defaultDeathMessage = "You died...";
        [SerializeField] private Sprite defaultDeathSprite;
        [SerializeField] private float defaultDisplayDuration = 4f;

        [Header("Text Settings")]
        [SerializeField] private TMP_FontAsset customFont; // Custom font
        [SerializeField] [Range(8, 100)] private float fontSize = 24f; // Font size
        [SerializeField] private Color textColor = Color.white; // Font color
        [SerializeField] private TextAlignmentOptions textAlignment = TextAlignmentOptions.Center; // Text alignment
        [SerializeField] private bool enableTextOutline = false; // Enable text outline (simple shadow effect)
        [SerializeField] private Color outlineColor = Color.black; // Outline/shadow color
        [SerializeField] [Range(0f, 5f)] private float outlineWidth = 1f; // Outline/shadow offset

        private Coroutine currentDisplayCoroutine;
        private RectTransform imageRectTransform;
        private Vector2 originalImageSize;

        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeComponents();
        }

        private void Start()
        {
            // Apply settings again in Start to ensure Inspector values are correctly applied
            ApplyCurrentSettings();
        }

        /// <summary>
        /// Apply current Inspector settings
        /// </summary>
        private void ApplyCurrentSettings()
        {
            SetImageMaxScreenRatio(imageMaxScreenRatio);
            SetBackgroundAlpha(backgroundAlpha);
            ApplyTextSettings();
            
            Debug.Log($"[GlobalDeathUI] Applied settings - Image screen ratio: {imageMaxScreenRatio}, Background alpha: {backgroundAlpha}, Font size: {fontSize}");
        }

        private void InitializeComponents()
        {
            // Auto-find components if not specified
            if (deathCanvas == null)
                deathCanvas = GetComponentInChildren<Canvas>();

            if (deathPanel == null)
                deathPanel = GetComponentInChildren<CanvasGroup>();

            if (deathImage == null)
                deathImage = GetComponentInChildren<Image>();

            if (deathMessageText == null)
                deathMessageText = GetComponentInChildren<TextMeshProUGUI>();

            if (backgroundOverlay == null)
            {
                Image[] images = GetComponentsInChildren<Image>();
                if (images.Length > 1)
                    backgroundOverlay = images[0]; // First one is usually the background
            }

            if (deathImage != null)
            {
                imageRectTransform = deathImage.GetComponent<RectTransform>();
                if (imageRectTransform != null)
                    originalImageSize = imageRectTransform.sizeDelta;
            }

            // Set initial state to hidden
            if (deathPanel != null)
            {
                deathPanel.alpha = 0f;
                deathPanel.interactable = false;
                deathPanel.blocksRaycasts = false;
            }

            // Set background transparency
            if (backgroundOverlay != null)
            {
                Color bgColor = backgroundOverlay.color;
                bgColor.a = backgroundAlpha;
                backgroundOverlay.color = bgColor;
            }
        }

        /// <summary>
        /// Show death UI
        /// </summary>
        /// <param name="message">Death message</param>
        /// <param name="sprite">Death image</param>
        /// <param name="duration">Display duration, <=0 uses default duration</param>
        public void ShowDeathUI(string message = null, Sprite sprite = null, float duration = -1f)
        {
            if (currentDisplayCoroutine != null)
            {
                StopCoroutine(currentDisplayCoroutine);
            }

            float displayTime = duration > 0 ? duration : defaultDisplayDuration;
            currentDisplayCoroutine = StartCoroutine(DisplayDeathUI(message, sprite, displayTime));
        }

        /// <summary>
        /// Hide death UI
        /// </summary>
        public void HideDeathUI()
        {
            if (currentDisplayCoroutine != null)
            {
                StopCoroutine(currentDisplayCoroutine);
                currentDisplayCoroutine = null;
            }
            StartCoroutine(FadeOut());
        }

        private IEnumerator DisplayDeathUI(string message, Sprite sprite, float duration)
        {
            // 设置内容
            SetupDeathContent(message, sprite);

            // 淡入
            yield return StartCoroutine(FadeIn());

            // 等待显示时间
            yield return new WaitForSecondsRealtime(duration);

            // 淡出
            yield return StartCoroutine(FadeOut());

            currentDisplayCoroutine = null;
        }

        private void SetupDeathContent(string message, Sprite sprite)
        {
            // 设置消息文本
            if (deathMessageText != null)
            {
                deathMessageText.text = string.IsNullOrEmpty(message) ? defaultDeathMessage : message;
            }

            // 设置死亡图片
            if (deathImage != null)
            {
                Sprite displaySprite = sprite != null ? sprite : defaultDeathSprite;
                deathImage.sprite = displaySprite;

                // 调整图片大小以适应屏幕中央限制
                if (displaySprite != null && imageRectTransform != null)
                {
                    AdjustImageSize(displaySprite);
                }
            }
        }

        private void AdjustImageSize(Sprite sprite)
        {
            if (imageRectTransform == null) return;

            // 获取屏幕尺寸
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            Vector2 maxAllowedSize = screenSize * imageMaxScreenRatio;

            // 可选的最大尺寸限制
            if (useImageMaxSizeLimit)
            {
                maxAllowedSize.x = Mathf.Min(maxAllowedSize.x, imageMaxSize.x);
                maxAllowedSize.y = Mathf.Min(maxAllowedSize.y, imageMaxSize.y);
            }

            // 获取原始图片尺寸
            Vector2 spriteSize = new Vector2(sprite.texture.width, sprite.texture.height);

            Vector2 finalSize;

            if (maintainAspectRatio)
            {
                // 保持宽高比，按最小缩放比例缩放
                float scaleX = maxAllowedSize.x / spriteSize.x;
                float scaleY = maxAllowedSize.y / spriteSize.y;
                float scale = Mathf.Min(scaleX, scaleY);

                finalSize = spriteSize * scale;
            }
            else
            {
                // 不保持宽高比，直接使用最大允许尺寸
                finalSize = maxAllowedSize;
            }

            imageRectTransform.sizeDelta = finalSize;

            Debug.Log($"[GlobalDeathUI] Adjust image size: Screen={screenSize}, Original={spriteSize}, Screen ratio={imageMaxScreenRatio}, Calculated size={maxAllowedSize}, Final size={finalSize}");
        }

        private IEnumerator FadeIn()
        {
            if (deathPanel == null) yield break;

            deathPanel.interactable = true;
            deathPanel.blocksRaycasts = true;

            float elapsedTime = 0f;
            while (elapsedTime < fadeInDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float progress = elapsedTime / fadeInDuration;
                float alpha = fadeCurve.Evaluate(progress);
                deathPanel.alpha = alpha;
                yield return null;
            }

            deathPanel.alpha = 1f;
        }

        private IEnumerator FadeOut()
        {
            if (deathPanel == null) yield break;

            float elapsedTime = 0f;
            float startAlpha = deathPanel.alpha;

            while (elapsedTime < fadeOutDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float progress = elapsedTime / fadeOutDuration;
                float alpha = startAlpha * (1f - fadeCurve.Evaluate(progress));
                deathPanel.alpha = alpha;
                yield return null;
            }

            deathPanel.alpha = 0f;
            deathPanel.interactable = false;
            deathPanel.blocksRaycasts = false;
        }

        /// <summary>
        /// Set image maximum screen ratio
        /// </summary>
        public void SetImageMaxScreenRatio(float ratio)
        {
            imageMaxScreenRatio = Mathf.Clamp01(ratio);
        }

        /// <summary>
        /// Set background transparency
        /// </summary>
        public void SetBackgroundAlpha(float alpha)
        {
            backgroundAlpha = Mathf.Clamp01(alpha);
            if (backgroundOverlay != null)
            {
                Color bgColor = backgroundOverlay.color;
                bgColor.a = backgroundAlpha;
                backgroundOverlay.color = bgColor;
            }
        }

        /// <summary>
        /// Apply text settings
        /// </summary>
        public void ApplyTextSettings()
        {
            if (deathMessageText == null) return;

            // Apply font
            if (customFont != null)
            {
                deathMessageText.font = customFont;
            }

            // Apply font size
            deathMessageText.fontSize = fontSize;

            // Apply font color
            deathMessageText.color = textColor;

            // Apply text alignment
            deathMessageText.alignment = textAlignment;

            // Apply shadow effect (simple outline alternative)
            ApplyTextShadowEffect();

            Debug.Log($"[GlobalDeathUI] Applied text settings - Font size: {fontSize}, Color: {textColor}, Outline: {enableTextOutline}");
        }

        /// <summary>
        /// Set font size
        /// </summary>
        public void SetFontSize(float size)
        {
            fontSize = Mathf.Clamp(size, 8f, 100f);
            if (deathMessageText != null)
            {
                deathMessageText.fontSize = fontSize;
            }
        }

        /// <summary>
        /// Set text color
        /// </summary>
        public void SetTextColor(Color color)
        {
            textColor = color;
            if (deathMessageText != null)
            {
                deathMessageText.color = textColor;
            }
        }

        /// <summary>
        /// Set custom font
        /// </summary>
        public void SetCustomFont(TMP_FontAsset font)
        {
            customFont = font;
            if (deathMessageText != null && customFont != null)
            {
                deathMessageText.font = customFont;
            }
        }

        /// <summary>
        /// Apply text shadow effect (simple outline alternative)
        /// </summary>
        private void ApplyTextShadowEffect()
        {
            if (deathMessageText == null) return;

            // Remove existing shadow component
            UnityEngine.UI.Shadow existingShadow = deathMessageText.GetComponent<UnityEngine.UI.Shadow>();
            if (existingShadow != null)
            {
                DestroyImmediate(existingShadow);
            }

            // Add shadow component if outline is enabled
            if (enableTextOutline)
            {
                UnityEngine.UI.Shadow shadow = deathMessageText.gameObject.AddComponent<UnityEngine.UI.Shadow>();
                shadow.effectColor = outlineColor;
                shadow.effectDistance = new Vector2(outlineWidth, -outlineWidth);
                shadow.useGraphicAlpha = true;

                Debug.Log($"[GlobalDeathUI] Applied text shadow - Color: {outlineColor}, Offset: {outlineWidth}");
            }
        }

        /// <summary>
        /// Force apply current settings
        /// </summary>
        [ContextMenu("Apply Current Settings")]
        public void ForceApplySettings()
        {
            ApplyCurrentSettings();
        }

        /// <summary>
        /// Create death UI at runtime (if not preset in scene)
        /// </summary>
        [ContextMenu("Create Death UI Runtime")]
        public void CreateDeathUIRuntime()
        {
            if (deathCanvas != null) return; // Already exists

            // Create Canvas
            GameObject canvasGO = new GameObject("GlobalDeathCanvas");
            canvasGO.transform.SetParent(transform);
            deathCanvas = canvasGO.AddComponent<Canvas>();
            deathCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            deathCanvas.sortingOrder = 1000; // Ensure on top layer

            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            // Create main panel
            GameObject panelGO = new GameObject("DeathPanel");
            panelGO.transform.SetParent(canvasGO.transform, false);
            
            // Add RectTransform component (required for UI objects)
            RectTransform panelRect = panelGO.AddComponent<RectTransform>();
            deathPanel = panelGO.AddComponent<CanvasGroup>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            panelRect.anchoredPosition = Vector2.zero;

            // Create background overlay
            GameObject bgGO = new GameObject("Background");
            bgGO.transform.SetParent(panelGO.transform, false);
            RectTransform bgRect = bgGO.AddComponent<RectTransform>();
            backgroundOverlay = bgGO.AddComponent<Image>();
            backgroundOverlay.color = new Color(0, 0, 0, backgroundAlpha);
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bgRect.anchoredPosition = Vector2.zero;

            // Create death image
            GameObject imageGO = new GameObject("DeathImage");
            imageGO.transform.SetParent(panelGO.transform, false);
            imageRectTransform = imageGO.AddComponent<RectTransform>();
            deathImage = imageGO.AddComponent<Image>();
            deathImage.preserveAspect = maintainAspectRatio;
            imageRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            imageRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            imageRectTransform.sizeDelta = imageMaxSize;
            imageRectTransform.anchoredPosition = Vector2.zero;

            // Create death message text
            GameObject textGO = new GameObject("DeathMessage");
            textGO.transform.SetParent(panelGO.transform, false);
            RectTransform textRect = textGO.AddComponent<RectTransform>();
            deathMessageText = textGO.AddComponent<TextMeshProUGUI>();
            deathMessageText.text = defaultDeathMessage;
            
            // Apply text settings
            ApplyTextSettings();
            textRect.anchorMin = new Vector2(0.1f, 0.1f);
            textRect.anchorMax = new Vector2(0.9f, 0.3f);
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            InitializeComponents();
            Debug.Log("[GlobalDeathUI] Runtime death UI creation completed");
        }
    }
}
