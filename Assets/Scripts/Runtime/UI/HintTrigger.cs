using UnityEngine;
using TMPro;
using System.Collections;
using Antventure.UI;

/// <summary>
/// Data structure for timed hints
/// </summary>
[System.Serializable]
public class TimedHintData
{
    public int remainingSeconds;
    [Header("Text Source")]
    public bool useTextComponent = false; // Use TextMeshPro component instead of string
    [TextArea] public string hintText; // Fallback text if no component
    public TextMeshProUGUI textComponent; // TextMeshPro component reference
}

/// <summary>
/// Component specifically responsible for triggering UI hints
/// Hint UI logic extracted from Checkpoint
/// </summary>
public class HintTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private bool triggerOnce = true; // Whether to trigger only once
    [SerializeField] private bool canRetrigger = false; // Whether can be retriggered
    [SerializeField] private bool requireEKeyPress = false; // Require E key press to trigger
    [SerializeField] private KeyCode triggerKey = KeyCode.E; // Key to trigger the hint
    
    [Header("Hint Text Settings")]
    [SerializeField] private string hintMessage = "Hint Message";
    [SerializeField] private TextMeshProUGUI hintTextComponent; // Local text component
    [SerializeField] private bool useTextFromComponent = false; // Use text from hintTextComponent instead of hintMessage
    [SerializeField] private float autoHideDelay = 5f; // Auto hide delay
    [SerializeField] private bool showHintOnTrigger = true; // Whether to show hint on trigger
    
    [Header("Local Hint Display")]
    [SerializeField] private bool useLocalHint = true; // Use local hint instead of UIManager
    [SerializeField] private bool allowRepeatHint = false; // Allow repeated hint display
    [SerializeField] private bool alwaysShowHint = false; // Always show hint
    [SerializeField] private bool showOnStart = false; // Show hint when game starts
    
    [Header("Dynamic Text Control")]
    [SerializeField] private bool enableDynamicControl = false; // Enable dynamic text control
    [SerializeField] private TextMeshProUGUI alternativeText; // Alternative text input field
    [SerializeField] private bool hideAfterChange = false; // Hide text after change
    [SerializeField] private float alternativeTextDuration = 5f; // Duration to show alternative text
    
    [Header("Climbing Event Control")]
    [SerializeField] private bool hideOnClimbingSuccess = false; // Hide hint when climbing succeeds
    
    [Header("Cookie Requirement Check")]
    [SerializeField] private bool checkCookieRequirement = false; // Check cookie requirement before triggering
    [SerializeField] private int requiredCookies = 3; // Required number of cookies
    [SerializeField] private bool showScreenMessage = true; // Show message on screen instead of local hint
    [SerializeField] private float screenMessageDuration = 2f; // Duration for screen message
    
    [Header("Custom Canvas for Cookie Message")]
    [SerializeField] private bool useCustomCanvas = false; // Use custom canvas instead of UIManager
    [SerializeField] private Canvas customCanvas; // Custom canvas for cookie message
    [SerializeField] private TextMeshProUGUI customMessageText; // Text component in custom canvas
    [SerializeField] private CanvasGroup customCanvasGroup; // Canvas group for fading animation
    
    [Header("Timed Hint System")]
    [SerializeField] private bool useTimedHints = false; // Enable timed hint system
    [SerializeField] private bool startTimedHintsOnStart = false; // Start timed hints when game starts
    [SerializeField] private float totalDuration = 60f; // Total duration in seconds
    [SerializeField] private TimedHintData[] timedHints = new TimedHintData[]
    {
        new TimedHintData { remainingSeconds = 40, hintText = "First warning!" },
        new TimedHintData { remainingSeconds = 20, hintText = "Second warning!" },
        new TimedHintData { remainingSeconds = 10, hintText = "Final warning!" }
    };
    
    // Local hint system
    private LocalHintDisplay localHintDisplay;
    
    // State tracking
    private bool hasTriggered = false;
    private bool hasTriggeredHint = false;
    private Coroutine hideCoroutine;
    private bool playerInRange = false; // Track if player is in trigger range
    
    // Dynamic text control
    private string originalText;
    private string currentDisplayText;
    
    // Timed hint system
    private bool isTimedSystemRunning = false;
    private double timedStartTime = 0;
    private bool[] timedHintShown;
    private Coroutine timedHintCoroutine;
    
    // Custom canvas system
    private Coroutine customCanvasCoroutine;
    
    void Start()
    {
        InitializeHintSystem();
        
        // Subscribe to climbing success event if enabled
        if (hideOnClimbingSuccess)
        {
            CheckpointUp.OnClimbingSuccess += OnClimbingSuccess;
        }
        
        // Check if collider is properly set up
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning($"HintTrigger {gameObject.name}: No Collider found, adding BoxCollider automatically");
            BoxCollider boxCol = gameObject.AddComponent<BoxCollider>();
            boxCol.isTrigger = true;
            boxCol.size = Vector3.one; // Default size
            col = boxCol;
        }
        
        if (!col.isTrigger)
        {
            Debug.LogWarning($"HintTrigger {gameObject.name}: Collider is not set as trigger, fixing it");
            col.isTrigger = true;
        }
        
        Debug.Log($"HintTrigger {gameObject.name}: Collider setup OK, isTrigger={col.isTrigger}");
    }
    
    private void InitializeHintSystem()
    {
        // Setup local hint
        SetupLocalHint();
        
        // Initialize text
        if (useTextFromComponent && hintTextComponent != null && !string.IsNullOrEmpty(hintTextComponent.text))
        {
            // Use text from the component
            originalText = hintTextComponent.text;
            hintMessage = originalText; // Update hintMessage to match
        }
        else
        {
            // Use hintMessage from inspector
            originalText = hintMessage;
        }
        currentDisplayText = originalText;
        
        // Initialize timed hint system
        if (useTimedHints && timedHints != null)
        {
            timedHintShown = new bool[timedHints.Length];
        }
        
        // If always show hint is enabled, show immediately
        if (alwaysShowHint && !string.IsNullOrEmpty(currentDisplayText))
        {
            ShowHint();
        }
        // If show on start is enabled, show hint with auto hide
        else if (showOnStart && !string.IsNullOrEmpty(currentDisplayText))
        {
            ShowHint();
        }
        
        // If start timed hints on start is enabled, start the timed system
        if (useTimedHints && startTimedHintsOnStart)
        {
            StartTimedHintSystem();
        }
        
        // Ensure custom canvas is initially hidden
        if (useCustomCanvas && customCanvas != null)
        {
            customCanvas.gameObject.SetActive(false);
            Debug.Log($"HintTrigger {gameObject.name}: Custom canvas initially hidden");
        }
    }
    
    private void SetupLocalHint()
    {
        if (useLocalHint)
        {
            // Get or create LocalHintDisplay component
            localHintDisplay = GetComponent<LocalHintDisplay>();
            if (localHintDisplay == null)
            {
                localHintDisplay = gameObject.AddComponent<LocalHintDisplay>();
            }
            
            // Configure local hint display
            localHintDisplay.SetDisplayDuration(autoHideDelay);
            localHintDisplay.SetAllowRepeat(allowRepeatHint);
        }
    }
    
    void Update()
    {
        // Check for key press if player is in range and E key is required
        if (requireEKeyPress && playerInRange && Input.GetKeyDown(triggerKey))
        {
            Debug.Log($"HintTrigger {gameObject.name}: E key pressed, triggering hint");
            TriggerHint();
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            playerInRange = true;
            Debug.Log($"HintTrigger {gameObject.name}: Player entered range, requireEKeyPress={requireEKeyPress}");
            
            // If E key is not required, trigger immediately
            if (!requireEKeyPress)
            {
                TriggerHint();
            }
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            playerInRange = false;
            Debug.Log($"HintTrigger {gameObject.name}: Player left range");
        }
    }
    
    public void TriggerHint()
    {
        // Check if can trigger
        if (triggerOnce && hasTriggered && !canRetrigger)
        {
            return;
        }
        
        // Check cookie requirement if enabled
        if (checkCookieRequirement && !HasEnoughCookies())
        {
            Debug.Log($"HintTrigger {gameObject.name}: Not enough cookies, showing message");
            ShowNotEnoughCookiesMessage();
            return;
        }
        
        // Start timed hint system if enabled and not set to start automatically
        if (useTimedHints && !startTimedHintsOnStart && !isTimedSystemRunning)
        {
            StartTimedHintSystem();
        }
        // Show hint text normally (only if not using timed hints or if timed hints start automatically)
        else if (showHintOnTrigger && (!hasTriggeredHint || canRetrigger || allowRepeatHint) && 
                 (!useTimedHints || startTimedHintsOnStart))
        {
            ShowHint();
            hasTriggeredHint = true;
        }
        
        hasTriggered = true;
        Debug.Log($"HintTrigger triggered: {gameObject.name}");
    }
    
    /// <summary>
    /// Show hint text
    /// </summary>
    public void ShowHint()
    {
        if (useLocalHint && localHintDisplay != null && !string.IsNullOrEmpty(currentDisplayText))
        {
            // Use local hint display
            localHintDisplay.ShowHint(currentDisplayText);
            Debug.Log($"Show local hint: {gameObject.name}, will auto hide after {autoHideDelay} seconds");
        }
        else if (hintTextComponent != null && !string.IsNullOrEmpty(currentDisplayText))
        {
            // Use specified text component
            ShowHintWithComponent();
        }
    }
    
    private void ShowHintWithComponent()
    {
        if (hintTextComponent != null)
        {
            hintTextComponent.text = currentDisplayText;
            hintTextComponent.gameObject.SetActive(true);
            
            // If not always show, set auto hide
            if (!alwaysShowHint)
            {
                if (hideCoroutine != null)
                {
                    StopCoroutine(hideCoroutine);
                }
                hideCoroutine = StartCoroutine(HideHintAfterDelay());
            }
        }
    }
    
    private IEnumerator HideHintAfterDelay()
    {
        yield return new WaitForSeconds(autoHideDelay);
        HideHint();
    }
    
    /// <summary>
    /// Hide hint text
    /// </summary>
    public void HideHint()
    {
        if (hintTextComponent != null)
        {
            hintTextComponent.gameObject.SetActive(false);
        }
        
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }
    }
    
    /// <summary>
    /// Force hide hint immediately, bypassing all delays and animations
    /// </summary>
    public void ForceHideHintImmediately()
    {
        // Stop any running coroutines
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }
        
        // Hide local hint display immediately
        if (useLocalHint && localHintDisplay != null)
        {
            localHintDisplay.HideHint();
        }
        
        // Hide text component immediately
        if (hintTextComponent != null)
        {
            hintTextComponent.gameObject.SetActive(false);
        }
        
        // Hide alternative text immediately
        if (alternativeText != null)
        {
            alternativeText.gameObject.SetActive(false);
        }
        
        // Disable the collider to prevent further triggering
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
            Debug.Log($"HintTrigger {gameObject.name}: Collider disabled (m_Enabled = 0)");
        }
    }
    
    /// <summary>
    /// Dynamically change hint text
    /// </summary>
    public void ChangeHintText(string newText)
    {
        if (!enableDynamicControl) return;
        
        currentDisplayText = newText;
        
        if (alternativeText != null)
        {
            alternativeText.text = newText;
            alternativeText.gameObject.SetActive(true);
            
            if (hideAfterChange)
            {
                StartCoroutine(HideAlternativeTextAfterDelay());
            }
        }
        
        // If currently showing hint, update display
        if (hintTextComponent != null && hintTextComponent.gameObject.activeInHierarchy)
        {
            ShowHint();
        }
    }
    
    private IEnumerator HideAlternativeTextAfterDelay()
    {
        yield return new WaitForSeconds(alternativeTextDuration);
        if (alternativeText != null)
        {
            alternativeText.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Reset trigger state
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggered = false;
        hasTriggeredHint = false;
        currentDisplayText = originalText;
        HideHint();
    }
    
    /// <summary>
    /// Force trigger (ignore trigger limitations)
    /// </summary>
    public void ForceTrigger()
    {
        bool originalTriggerOnce = triggerOnce;
        bool originalCanRetrigger = canRetrigger;
        
        triggerOnce = false;
        canRetrigger = true;
        
        TriggerHint();
        
        triggerOnce = originalTriggerOnce;
        canRetrigger = originalCanRetrigger;
    }
    
    /// <summary>
    /// Set hint message
    /// </summary>
    public void SetHintMessage(string message)
    {
        hintMessage = message;
        originalText = message;
        currentDisplayText = message;
    }
    
    /// <summary>
    /// Get current hint message
    /// </summary>
    public string GetHintMessage()
    {
        return currentDisplayText;
    }
    
    /// <summary>
    /// Refresh text from component if useTextFromComponent is enabled
    /// </summary>
    public void RefreshTextFromComponent()
    {
        if (useTextFromComponent && hintTextComponent != null && !string.IsNullOrEmpty(hintTextComponent.text))
        {
            originalText = hintTextComponent.text;
            currentDisplayText = originalText;
            hintMessage = originalText; // Keep inspector in sync
            Debug.Log($"HintTrigger {gameObject.name}: Text refreshed from component: {originalText}");
        }
    }
    
    /// <summary>
    /// Start the timed hint system
    /// </summary>
    public void StartTimedHintSystem()
    {
        if (isTimedSystemRunning) return;
        
        isTimedSystemRunning = true;
        timedStartTime = Time.timeAsDouble;
        
        // Reset all hint shown flags
        if (timedHintShown != null)
        {
            for (int i = 0; i < timedHintShown.Length; i++)
            {
                timedHintShown[i] = false;
            }
        }
        
        // Start the timed hint coroutine
        if (timedHintCoroutine != null)
        {
            StopCoroutine(timedHintCoroutine);
        }
        timedHintCoroutine = StartCoroutine(RunTimedHints());
        
        Debug.Log($"HintTrigger {gameObject.name}: Timed hint system started for {totalDuration} seconds");
    }
    
    /// <summary>
    /// Stop the timed hint system
    /// </summary>
    public void StopTimedHintSystem()
    {
        if (!isTimedSystemRunning) return;
        
        isTimedSystemRunning = false;
        
        if (timedHintCoroutine != null)
        {
            StopCoroutine(timedHintCoroutine);
            timedHintCoroutine = null;
        }
        
        Debug.Log($"HintTrigger {gameObject.name}: Timed hint system stopped");
    }
    
    /// <summary>
    /// Coroutine that runs the timed hint system
    /// </summary>
    private IEnumerator RunTimedHints()
    {
        while (isTimedSystemRunning)
        {
            double elapsed = Time.timeAsDouble - timedStartTime;
            int remainingSeconds = Mathf.Max(0, Mathf.CeilToInt((float)(totalDuration - elapsed)));
            
            // Check each timed hint
            for (int i = 0; i < timedHints.Length; i++)
            {
                if (!timedHintShown[i] && remainingSeconds <= timedHints[i].remainingSeconds)
                {
                    timedHintShown[i] = true;
                    ShowTimedHint(timedHints[i]);
                }
            }
            
            // Check if time is up
            if (elapsed >= totalDuration)
            {
                StopTimedHintSystem();
                yield break;
            }
            
            yield return null; // Wait for next frame
        }
    }
    
    /// <summary>
    /// Show a timed hint with TimedHintData
    /// </summary>
    private void ShowTimedHint(TimedHintData hintData)
    {
        string textToShow = GetTextFromHintData(hintData);
        
        if (string.IsNullOrEmpty(textToShow))
        {
            Debug.LogWarning($"HintTrigger {gameObject.name}: No text to show for timed hint");
            return;
        }
        
        if (useLocalHint && localHintDisplay != null)
        {
            localHintDisplay.ShowHint(textToShow);
            Debug.Log($"HintTrigger {gameObject.name}: Timed hint shown (local): {textToShow}");
        }
        else if (hintTextComponent != null)
        {
            hintTextComponent.text = textToShow;
            hintTextComponent.gameObject.SetActive(true);
            Debug.Log($"HintTrigger {gameObject.name}: Timed hint shown (component): {textToShow}");
        }
    }
    
    /// <summary>
    /// Get text from TimedHintData, either from component or fallback string
    /// </summary>
    private string GetTextFromHintData(TimedHintData hintData)
    {
        if (hintData.useTextComponent && hintData.textComponent != null && !string.IsNullOrEmpty(hintData.textComponent.text))
        {
            return hintData.textComponent.text;
        }
        else
        {
            return hintData.hintText;
        }
    }
    
    /// <summary>
    /// Check if player has enough cookies
    /// </summary>
    private bool HasEnoughCookies()
    {
        if (CookiesInventory.Instance == null)
        {
            Debug.LogWarning($"HintTrigger {gameObject.name}: CookiesInventory.Instance is null");
            return false;
        }
        
        int currentCookies = CookiesInventory.Instance.cookies;
        bool hasEnough = currentCookies >= requiredCookies;
        Debug.Log($"HintTrigger {gameObject.name}: Cookie check - current={currentCookies}, required={requiredCookies}, hasEnough={hasEnough}");
        
        return hasEnough;
    }
    
    /// <summary>
    /// Show not enough cookies message
    /// </summary>
    private void ShowNotEnoughCookiesMessage()
    {
        if (useCustomCanvas && customCanvas != null && customMessageText != null)
        {
            // Use custom canvas
            ShowCustomCanvasMessage();
        }
        else if (showScreenMessage)
        {
            // Show message on screen using UIManager
            if (UIManager.Instance != null)
            {
                string messageText = customMessageText != null ? customMessageText.text : "You need 3 cookies";
                UIManager.Instance.ShowHintText(messageText, screenMessageDuration);
                Debug.Log($"HintTrigger {gameObject.name}: Screen message shown - {messageText}");
            }
            else
            {
                Debug.LogWarning($"HintTrigger {gameObject.name}: UIManager.Instance is null, cannot show screen message");
            }
        }
        else
        {
            // Show message locally
            if (useLocalHint && localHintDisplay != null)
            {
                string messageText = customMessageText != null ? customMessageText.text : "You need 3 cookies";
                localHintDisplay.ShowHint(messageText);
                Debug.Log($"HintTrigger {gameObject.name}: Local message shown - {messageText}");
            }
            else if (hintTextComponent != null)
            {
                string messageText = customMessageText != null ? customMessageText.text : "You need 3 cookies";
                hintTextComponent.text = messageText;
                hintTextComponent.gameObject.SetActive(true);
                Debug.Log($"HintTrigger {gameObject.name}: Component message shown - {messageText}");
            }
        }
    }
    
    /// <summary>
    /// Show message using custom canvas
    /// </summary>
    private void ShowCustomCanvasMessage()
    {
        // Debug canvas setup
        Debug.Log($"HintTrigger {gameObject.name}: Custom canvas setup check:");
        Debug.Log($"  - customCanvas: {(customCanvas != null ? customCanvas.name : "NULL")}");
        Debug.Log($"  - customMessageText: {(customMessageText != null ? customMessageText.name : "NULL")}");
        Debug.Log($"  - customCanvasGroup: {(customCanvasGroup != null ? customCanvasGroup.name : "NULL")}");
        
        if (customCanvas != null)
        {
            Debug.Log($"  - Canvas active: {customCanvas.gameObject.activeInHierarchy}");
            Debug.Log($"  - Canvas enabled: {customCanvas.enabled}");
            Debug.Log($"  - Canvas renderMode: {customCanvas.renderMode}");
            Debug.Log($"  - Canvas sortingOrder: {customCanvas.sortingOrder}");
            Debug.Log($"  - Canvas worldCamera: {(customCanvas.worldCamera != null ? customCanvas.worldCamera.name : "NULL")}");
        }
        
        if (customMessageText != null)
        {
            Debug.Log($"  - Text color: {customMessageText.color}");
            Debug.Log($"  - Text fontSize: {customMessageText.fontSize}");
            Debug.Log($"  - Text active: {customMessageText.gameObject.activeInHierarchy}");
            Debug.Log($"  - Text enabled: {customMessageText.enabled}");
            Debug.Log($"  - Text position: {customMessageText.transform.position}");
        }
        
        if (customCanvasCoroutine != null)
        {
            StopCoroutine(customCanvasCoroutine);
        }
        
        customCanvasCoroutine = StartCoroutine(ShowCustomCanvasCoroutine());
        Debug.Log($"HintTrigger {gameObject.name}: Custom canvas message shown - {customMessageText.text}");
    }
    
    /// <summary>
    /// Coroutine to handle custom canvas display with fade animation
    /// </summary>
    private IEnumerator ShowCustomCanvasCoroutine()
    {
        Debug.Log($"HintTrigger {gameObject.name}: Starting custom canvas coroutine");
        
        // Text content is already set in the TextMeshPro component
        if (customMessageText != null)
        {
            Debug.Log($"HintTrigger {gameObject.name}: Using existing text: {customMessageText.text}");
        }
        else
        {
            Debug.LogError($"HintTrigger {gameObject.name}: customMessageText is null!");
            yield break;
        }
        
        // Show canvas
        if (customCanvas != null)
        {
            customCanvas.gameObject.SetActive(true);
            customCanvas.enabled = true;
            
            // Force canvas to front
            customCanvas.sortingOrder = 9999;
            customCanvas.overrideSorting = true;
            
            Debug.Log($"HintTrigger {gameObject.name}: Canvas activated with sortingOrder: {customCanvas.sortingOrder}");
        }
        else
        {
            Debug.LogError($"HintTrigger {gameObject.name}: customCanvas is null!");
            yield break;
        }
        
        // Force text to be visible
        if (customMessageText != null)
        {
            customMessageText.gameObject.SetActive(true);
            customMessageText.enabled = true;
            Debug.Log($"HintTrigger {gameObject.name}: Text component activated");
        }
        
        // Fade in animation
        if (customCanvasGroup != null)
        {
            customCanvasGroup.alpha = 0f;
            float fadeInTime = 0.3f;
            float elapsedTime = 0f;
            
            Debug.Log($"HintTrigger {gameObject.name}: Starting fade in");
            while (elapsedTime < fadeInTime)
            {
                elapsedTime += Time.deltaTime;
                customCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeInTime);
                yield return null;
            }
            customCanvasGroup.alpha = 1f;
            Debug.Log($"HintTrigger {gameObject.name}: Fade in complete");
        }
        else
        {
            Debug.Log($"HintTrigger {gameObject.name}: No CanvasGroup, showing without fade");
        }
        
        // Wait for display duration
        Debug.Log($"HintTrigger {gameObject.name}: Waiting for {screenMessageDuration} seconds");
        Debug.Log($"HintTrigger {gameObject.name}: Canvas should be visible now! Check the screen!");
        Debug.Log($"HintTrigger {gameObject.name}: Screen resolution: {Screen.width}x{Screen.height}");
        Debug.Log($"HintTrigger {gameObject.name}: Text should be at position {customMessageText.transform.position}");
        Debug.Log($"HintTrigger {gameObject.name}: Text rect: {customMessageText.rectTransform.rect}");
        Debug.Log($"HintTrigger {gameObject.name}: Text anchored position: {customMessageText.rectTransform.anchoredPosition}");
        
        yield return new WaitForSeconds(screenMessageDuration);
        
        // Fade out animation
        if (customCanvasGroup != null)
        {
            float fadeOutTime = 0.3f;
            float elapsedTime = 0f;
            
            Debug.Log($"HintTrigger {gameObject.name}: Starting fade out");
            while (elapsedTime < fadeOutTime)
            {
                elapsedTime += Time.deltaTime;
                customCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutTime);
                yield return null;
            }
            customCanvasGroup.alpha = 0f;
            Debug.Log($"HintTrigger {gameObject.name}: Fade out complete");
        }
        
        // Hide canvas
        customCanvas.gameObject.SetActive(false);
        Debug.Log($"HintTrigger {gameObject.name}: Canvas deactivated");
        customCanvasCoroutine = null;
    }
    
    /// <summary>
    /// Handle climbing success event
    /// </summary>
    private void OnClimbingSuccess()
    {
        if (hideOnClimbingSuccess)
        {
            // Force immediate hide, bypass any delay
            ForceHideHintImmediately();
            Debug.Log($"HintTrigger {gameObject.name}: Immediately hidden due to climbing success");
        }
    }
    
    private void OnDisable()
    {
        HideHint();
        StopTimedHintSystem();
        
        // Stop custom canvas coroutine
        if (customCanvasCoroutine != null)
        {
            StopCoroutine(customCanvasCoroutine);
            customCanvasCoroutine = null;
        }
        
        // Unsubscribe from climbing success event
        if (hideOnClimbingSuccess)
        {
            CheckpointUp.OnClimbingSuccess -= OnClimbingSuccess;
        }
    }
    
    private void OnDestroy()
    {
        StopTimedHintSystem();
        
        // Stop custom canvas coroutine
        if (customCanvasCoroutine != null)
        {
            StopCoroutine(customCanvasCoroutine);
            customCanvasCoroutine = null;
        }
        
        // Unsubscribe from climbing success event
        if (hideOnClimbingSuccess)
        {
            CheckpointUp.OnClimbingSuccess -= OnClimbingSuccess;
        }
    }

    // Editor visualization
    void OnDrawGizmosSelected()
    {
        // Draw trigger area
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = hasTriggered ? Color.green : Color.yellow;
            Gizmos.matrix = transform.localToWorldMatrix;
            
            if (col is BoxCollider box)
            {
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            }
        }
    }
}
