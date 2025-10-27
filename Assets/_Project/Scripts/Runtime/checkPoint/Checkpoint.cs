using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Antventure.UI;
using System.Collections;

public class Checkpoint : MonoBehaviour
{
    [Header("Basic settings")]
    [SerializeField] private bool _isActivated = false;
    public bool canReactivate = false; // can be reactivated or not
    
    // Property to handle isActivated changes
    public bool isActivated 
    { 
        get => _isActivated; 
        set 
        { 
            if (_isActivated != value)
            {
                _isActivated = value;
                OnActivationChanged(value);
            }
        } 
    }
    
    [Header("Setting of Prompt Text")]
    [SerializeField] private TextMeshProUGUI hintTextComponent; // Text component for hint display
    [SerializeField] private float autoHideDelay = 5f; // Automatic disappearance time (seconds)
    [SerializeField] private bool showHintOnActivation = true; // Whether to display a prompt when activated
    
    [Header("Local Hint Display")]
    [SerializeField] private bool useLocalHint = true; // Use local hint instead of UIManager
    [SerializeField] private bool allowRepeatHint = false; // Allow hint to be shown multiple times
    [SerializeField] private bool alwaysShowHint = false; // Always show hint permanently
    
    [Header("Dynamic Text Control")]
    [SerializeField] private bool enableDynamicControl = false; // Enable dynamic text control
    [SerializeField] private TextMeshProUGUI alternativeText; // Input field for alternative text
    [SerializeField] private bool hideAfterChange = false; // Hide text after changing
    [SerializeField] private float alternativeTextDuration = 5f; // Duration to show alternative text
    
    [Header("Visual component")]
    private ParticleSystem particles;
    private Light checkpointLight;
    
    // Local hint system
    private LocalHintDisplay localHintDisplay;
    
    private bool hasTriggeredHint = false;
    private Coroutine hideCoroutine;
    
    // Dynamic text control
    private bool isTextChanged = false;
    private string currentDisplayText;

    void Start()
    {
        particles = GetComponentInChildren<ParticleSystem>();
        checkpointLight = GetComponentInChildren<Light>();
        
        // Auto-find hint text component if not assigned
        if (hintTextComponent == null)
        {
            hintTextComponent = GetComponentInChildren<TextMeshProUGUI>();
        }
        
        // Initialize current display text from component
        if (hintTextComponent != null)
        {
            currentDisplayText = hintTextComponent.text;
        }
        else
        {
            currentDisplayText = "Default Hint Text";
        }
        
        // Hide alternative text component initially
        if (alternativeText != null)
        {
            alternativeText.gameObject.SetActive(false);
        }
        
        // Setup local hint system
        SetupLocalHint();
        
        // initial
        SetActivationVisuals(false);
        
        // Always show hint if enabled (permanent) OR if checkpoint is pre-activated (timed)
        if (alwaysShowHint)
        {
            ShowPermanentHint();
        }
        else if (isActivated)
        {
            // Show hint for the set duration when pre-activated
            ShowCheckpointHint();
        }
    }
    
    /// <summary>
    /// Setup local hint display system
    /// </summary>
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
            
            // Configure the local hint display
            localHintDisplay.SetDisplayDuration(autoHideDelay);
            localHintDisplay.SetAllowRepeat(allowRepeatHint);
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ActivateCheckpoint();
            
            // Display prompt text
            if (showHintOnActivation && (!hasTriggeredHint || canReactivate))
            {
                ShowCheckpointHint();
                hasTriggeredHint = true;
            }
        }
    }
    
    public void ActivateCheckpoint()
    {
        // If it has already been activated and cannot be re-activated, then simply return.
        if (isActivated && !canReactivate)
        {
            SetActivationVisuals(true);
            return;
        }
        
        if (!isActivated)
        {
            // First activation
            isActivated = true;
            SetActivationVisuals(true);
            
            // Notify the archive point manager that this point has been activated (as a new archive point)
            CheckpointManager.Instance?.SetCheckpointActivated(this);
            
            // Show hint for the set duration when activated
            if (!alwaysShowHint) // Only if not already showing permanent hint
            {
                ShowCheckpointHint();
            }
            
            Debug.Log("Checkpoint activated for the first time: " + gameObject.name);
        }
        else if (canReactivate)
        {
            // The situation where activation can be repeated
            CheckpointManager.Instance?.SetCheckpointActivated(this);
            
            // Show hint for the set duration when reactivated
            if (!alwaysShowHint && allowRepeatHint)
            {
                ShowCheckpointHint();
            }
            
            Debug.Log("Checkpoint reactivated: " + gameObject.name);
        }
    }
    
    /// <summary>
    /// Display the checkpoint prompt text
    /// </summary>
    public void ShowCheckpointHint()
    {
        if (useLocalHint && localHintDisplay != null && !string.IsNullOrEmpty(currentDisplayText))
        {
            // Use local hint display
            localHintDisplay.ShowHint(currentDisplayText);
            Debug.Log($"Display local checkpoint prompt: {gameObject.name}, will auto-hide in {autoHideDelay} seconds");
        }
        else if (UIManager.Instance != null && !string.IsNullOrEmpty(currentDisplayText))
        {
            // Fallback to UIManager
            UIManager.Instance.ShowHintText(currentDisplayText, autoHideDelay);
            Debug.Log($"Display UI checkpoint prompt: {gameObject.name}, will auto-hide in {autoHideDelay} seconds");
        }
    }


    /// <summary>
    /// Display the checkpoint prompt text with custom text
    /// </summary>
    public void ShowCheckpointHint(string customHintText)
    {
        if (useLocalHint && localHintDisplay != null && !string.IsNullOrEmpty(customHintText))
        {
            // Use local hint display
            localHintDisplay.ShowHint(customHintText);
            Debug.Log($"Display custom local checkpoint prompt: {gameObject.name}, will auto-hide in {autoHideDelay} seconds");
        }
        else if (UIManager.Instance != null && !string.IsNullOrEmpty(customHintText))
        {
            // Fallback to UIManager
            UIManager.Instance.ShowHintText(customHintText, autoHideDelay);
            Debug.Log($"Display custom UI checkpoint prompt: {gameObject.name}, will auto-hide in {autoHideDelay} seconds");
        }
    }

    
    private IEnumerator AutoHideAfterDelay()
    {
        // Wait for the specified time
        yield return new WaitForSeconds(autoHideDelay);
        
        // Hide the hint text
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideHintText();
            Debug.Log($"Automatic hidden checkpoint prompt: {gameObject.name}");
        }
        
        hideCoroutine = null;
    }
    
    private void SetActivationVisuals(bool activated)
    {
        // Control particle effects
        if (particles != null)
        {
            if (activated)
                particles.Play();
            else
                particles.Stop();
        }
        
        // Control light
        if (checkpointLight != null)
        {
            checkpointLight.color = activated ? Color.green : Color.gray;
        }
    }
    
    // Reset the status of the saved points
    public void ResetCheckpoint()
    {
        isActivated = false;
        SetActivationVisuals(false);
        hasTriggeredHint = false;
        
        // Reset local hint state
        if (localHintDisplay != null)
        {
            localHintDisplay.ResetHintState();
        }
    }
    
    // force active
    public void ForceActivate()
    {
        isActivated = true;
        SetActivationVisuals(true);
        CheckpointManager.Instance?.SetCheckpointActivated(this);
        
        // force show hint
        ShowCheckpointHint();
    }
    
    // Manually immediately hide the prompt
    public void HideHint()
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }
        
        if (useLocalHint && localHintDisplay != null)
        {
            localHintDisplay.HideHint();
        }
        else if (UIManager.Instance != null)
        {
            UIManager.Instance.HideHintText();
        }
    }
    
    // Set the automatic hiding time (which can be adjusted during runtime)
    public void SetAutoHideDelay(float delay)
    {
        autoHideDelay = delay;
        if (localHintDisplay != null)
        {
            localHintDisplay.SetDisplayDuration(delay);
        }
    }
    
    // Set the prompt text (which can be adjusted during runtime)
    public void SetHintText(string newHintText)
    {
        if (hintTextComponent != null)
        {
            hintTextComponent.text = newHintText;
        }
        currentDisplayText = newHintText;
        
        // Update display if always showing
        if (alwaysShowHint)
        {
            ShowPermanentHint();
        }
    }
    
    // When an object is disabled, make sure to hide the prompt.
    private void OnDisable()
    {
        HideHint();
    }
    
    /// <summary>
    /// Set whether to use local hint or UIManager hint
    /// </summary>
    public void SetUseLocalHint(bool useLocal)
    {
        useLocalHint = useLocal;
        if (useLocal)
        {
            SetupLocalHint();
        }
    }
    
    /// <summary>
    /// Set whether hint can be repeated
    /// </summary>
    public void SetAllowRepeatHint(bool allowRepeat)
    {
        allowRepeatHint = allowRepeat;
        if (localHintDisplay != null)
        {
            localHintDisplay.SetAllowRepeat(allowRepeat);
        }
    }
    
    /// <summary>
    /// Get local hint display component
    /// </summary>
    public LocalHintDisplay GetLocalHintDisplay()
    {
        return localHintDisplay;
    }
    
    /// <summary>
    /// Check if hint is currently displaying
    /// </summary>
    public bool IsHintDisplaying()
    {
        if (useLocalHint && localHintDisplay != null)
        {
            return localHintDisplay.IsDisplaying;
        }
        return false;
    }
    
    /// <summary>
    /// Show hint permanently without any time limit or logic restrictions
    /// </summary>
    private void ShowPermanentHint()
    {
        if (!useLocalHint)
        {
            // Use the assigned hint text component first
            if (hintTextComponent != null)
            {
                hintTextComponent.text = currentDisplayText;
                hintTextComponent.gameObject.SetActive(true);
                hintTextComponent.alpha = 1f;
            }
            else
            {
                // Fallback: find any text component
                var tmpText = GetComponentInChildren<TextMeshProUGUI>();
                if (tmpText != null)
                {
                    tmpText.text = currentDisplayText;
                    tmpText.gameObject.SetActive(true);
                    tmpText.alpha = 1f;
                }
                
                var legacyText = GetComponentInChildren<Text>();
                if (legacyText != null)
                {
                    legacyText.text = currentDisplayText;
                    legacyText.gameObject.SetActive(true);
                    Color color = legacyText.color;
                    color.a = 1f;
                    legacyText.color = color;
                }
            }
        }
        else if (localHintDisplay != null)
        {
            // Force show using LocalHintDisplay but override its hiding logic
            localHintDisplay.SetAllowRepeat(true);
            localHintDisplay.ResetHintState();
            localHintDisplay.ShowHint(currentDisplayText);
            
            // Override the display duration to be very long
            localHintDisplay.SetDisplayDuration(999999f);
        }
    }
    
    /// <summary>
    /// Change the displayed text dynamically
    /// </summary>
    public void ChangeHintText(string newText)
    {
        if (!enableDynamicControl) return;
        
        currentDisplayText = newText;
        isTextChanged = true;
        
        // Update the display immediately if always showing
        if (alwaysShowHint)
        {
            ShowPermanentHint();
        }
    }
    
    /// <summary>
    /// Hide the hint text completely
    /// </summary>
    public void HideHintText()
    {
        if (!enableDynamicControl) return;
        
        // Use the assigned hint text component first
        if (hintTextComponent != null)
        {
            hintTextComponent.gameObject.SetActive(false);
        }
        else
        {
            // Fallback: find and hide any text component
            var tmpText = GetComponentInChildren<TextMeshProUGUI>();
            if (tmpText != null)
            {
                tmpText.gameObject.SetActive(false);
            }
            
            var legacyText = GetComponentInChildren<Text>();
            if (legacyText != null)
            {
                legacyText.gameObject.SetActive(false);
            }
        }
        
        if (localHintDisplay != null)
        {
            localHintDisplay.HideHint();
        }
    }
    
    /// <summary>
    /// Switch to alternative text from input field
    /// </summary>
    public void SwitchToAlternativeText()
    {
        if (!enableDynamicControl || alternativeText == null) return;
        
        string inputText = alternativeText.text;
        if (string.IsNullOrEmpty(inputText)) return;
        
        // Use the new method with duration control
        ShowAlternativeTextWithDuration(inputText);
    }
    
    /// <summary>
    /// Reset to original text
    /// </summary>
    public void ResetToOriginalText()
    {
        if (!enableDynamicControl) return;
        
        // Get original text from the text component
        if (hintTextComponent != null)
        {
            // Reset to the original text that was set in the component
            currentDisplayText = hintTextComponent.text;
        }
        
        isTextChanged = false;
        
        if (alwaysShowHint)
        {
            ShowPermanentHint();
        }
    }
    
    /// <summary>
    /// Update text from input field in real-time (can be called from input field's OnValueChanged event)
    /// </summary>
    public void UpdateTextFromInput()
    {
        if (!enableDynamicControl || alternativeText == null) return;
        
        string inputText = alternativeText.text;
        if (string.IsNullOrEmpty(inputText))
        {
            ResetToOriginalText();
        }
        else
        {
            ChangeHintText(inputText);
        }
    }
    
    /// <summary>
    /// Show alternative text with duration control
    /// </summary>
    public void ShowAlternativeTextWithDuration(string text)
    {
        if (!enableDynamicControl || alternativeText == null) return;
        
        // Hide original hint text first
        HideOriginalHintText();
        
        // Set text and show alternative text component
        alternativeText.text = text;
        alternativeText.gameObject.SetActive(true);
        
        // Schedule hiding alternative text after duration
        Invoke(nameof(HideAlternativeText), alternativeTextDuration);
    }
    
    /// <summary>
    /// Show alternative text with custom duration
    /// </summary>
    public void ShowAlternativeTextWithDuration(string text, float duration)
    {
        if (!enableDynamicControl || alternativeText == null) return;
        
        // Hide original hint text first
        HideOriginalHintText();
        
        // Set text and show alternative text component
        alternativeText.text = text;
        alternativeText.gameObject.SetActive(true);
        
        // Schedule hiding alternative text after custom duration
        Invoke(nameof(HideAlternativeText), duration);
    }
    
    /// <summary>
    /// Show alternative text permanently (replaces original hint text)
    /// </summary>
    public void ShowAlternativeTextPermanently(string text)
    {
        Debug.Log($"Checkpoint: ShowAlternativeTextPermanently called with text: {text}");
        Debug.Log($"Checkpoint: enableDynamicControl = {enableDynamicControl}, alternativeText = {alternativeText}");
        
        if (!enableDynamicControl || alternativeText == null) 
        {
            Debug.Log("Checkpoint: Early return - dynamic control disabled or alternativeText is null");
            return;
        }
        
        // Hide original hint text first
        HideOriginalHintText();
        
        // Set text and show alternative text component permanently
        alternativeText.text = text;
        alternativeText.alpha = 1f; // Ensure it's fully visible
        
        // Ensure the GameObject and all parents are active
        alternativeText.gameObject.SetActive(true);
        
        // Check if parent objects are active
        Transform parent = alternativeText.transform.parent;
        while (parent != null)
        {
            Debug.Log($"Checkpoint: Parent {parent.name} active: {parent.gameObject.activeSelf}");
            if (!parent.gameObject.activeSelf)
            {
                Debug.Log($"Checkpoint: Activating parent {parent.name}");
                parent.gameObject.SetActive(true);
            }
            parent = parent.parent;
        }
        
        Debug.Log($"Checkpoint: Alternative text set to active with text: {alternativeText.text}");
        Debug.Log($"Checkpoint: Alternative text GameObject active: {alternativeText.gameObject.activeInHierarchy}");
        Debug.Log($"Checkpoint: Alternative text position: {alternativeText.transform.position}");
        Debug.Log($"Checkpoint: Alternative text alpha: {alternativeText.alpha}");
        
        // Check Canvas and CanvasRenderer
        Canvas canvas = alternativeText.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            Debug.Log($"Checkpoint: Canvas found: {canvas.name}, enabled: {canvas.enabled}, renderMode: {canvas.renderMode}");
        }
        else
        {
            Debug.Log("Checkpoint: No Canvas found in parent hierarchy!");
        }
        
        CanvasRenderer canvasRenderer = alternativeText.GetComponent<CanvasRenderer>();
        if (canvasRenderer != null)
        {
            Debug.Log($"Checkpoint: CanvasRenderer found, cull: {canvasRenderer.cull}");
        }
        else
        {
            Debug.Log("Checkpoint: No CanvasRenderer found!");
        }
        
        // No invoke to hide - it stays permanently
    }
    
    /// <summary>
    /// Hide alternative text component
    /// </summary>
    private void HideAlternativeText()
    {
        if (alternativeText != null)
        {
            alternativeText.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Hide original hint text component
    /// </summary>
    private void HideOriginalHintText()
    {
        Debug.Log("Checkpoint: HideOriginalHintText called");
        
        if (hintTextComponent != null)
        {
            Debug.Log($"Checkpoint: Hiding original hint text component: {hintTextComponent.name}");
            hintTextComponent.gameObject.SetActive(false);
        }
        
        // Also hide local hint display if using it
        if (useLocalHint && localHintDisplay != null)
        {
            Debug.Log("Checkpoint: Hiding local hint display");
            localHintDisplay.HideHint();
        }
    }
    
    /// <summary>
    /// Handle activation state changes
    /// </summary>
    private void OnActivationChanged(bool activated)
    {
        Debug.Log($"Checkpoint: Activation changed to {activated} for {gameObject.name}");
        
        if (activated)
        {
            // When activated, show hint for the set duration
            if (!alwaysShowHint)
            {
                ShowCheckpointHint();
            }
        }
        else
        {
            // When deactivated, hide the hint
            HideHint();
        }
    }
}