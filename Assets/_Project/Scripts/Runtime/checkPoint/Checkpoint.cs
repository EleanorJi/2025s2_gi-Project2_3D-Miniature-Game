using UnityEngine;
using Antventure.UI;
using System.Collections;

public class Checkpoint : MonoBehaviour
{
    [Header("Basic settings")]
    public bool isActivated = false;
    public bool canReactivate = false; // can be reactivated or not
    
    [Header("Setting of Prompt Text")]
    [TextArea(3, 6)]
    [SerializeField] private string hintText = "Please enter the prompt text.";
    [SerializeField] private float autoHideDelay = 5f; // Automatic disappearance time (seconds)
    [SerializeField] private bool showHintOnActivation = true; // Whether to display a prompt when activated
    
    [Header("Visual component")]
    private ParticleSystem particles;
    private Light checkpointLight;
    
    private bool hasTriggeredHint = false;
    private Coroutine hideCoroutine;

    void Start()
    {
        particles = GetComponentInChildren<ParticleSystem>();
        checkpointLight = GetComponentInChildren<Light>();
        
        // initial
        SetActivationVisuals(false);
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
            return;
        }
        
        if (!isActivated)
        {
            // First activation
            isActivated = true;
            SetActivationVisuals(true);
            
            // Notify the archive point manager that this point has been activated (as a new archive point)
            CheckpointManager.Instance?.SetCheckpointActivated(this);
            
            Debug.Log("Checkpoint activated for the first time: " + gameObject.name);
        }
        else if (canReactivate)
        {
            // The situation where activation can be repeated
            CheckpointManager.Instance?.SetCheckpointActivated(this);
            Debug.Log("Checkpoint reactivated: " + gameObject.name);
        }
    }
    
    /// <summary>
    /// Display the checkpoint prompt text
    /// </summary>
    public void ShowCheckpointHint()
    {
        if (UIManager.Instance != null && !string.IsNullOrEmpty(hintText))
        {
            // 使用 UIManager 的新方法，传递自动隐藏延迟时间
            UIManager.Instance.ShowHintText(hintText, autoHideDelay);

            Debug.Log($"Display checkpoint prompt: {gameObject.name}, will auto-hide in {autoHideDelay} seconds");
        }
    }


    /// <summary>
    /// Display the checkpoint prompt text with custom text
    /// </summary>
    public void ShowCheckpointHint(string customHintText)
    {
        if (UIManager.Instance != null && !string.IsNullOrEmpty(customHintText))
        {
            // 使用 UIManager 的新方法，传递自动隐藏延迟时间
            UIManager.Instance.ShowHintText(customHintText, autoHideDelay);

            Debug.Log($"Display custom checkpoint prompt: {gameObject.name}, will auto-hide in {autoHideDelay} seconds");
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
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideHintText();
        }
    }
    
    // Set the automatic hiding time (which can be adjusted during runtime)
    public void SetAutoHideDelay(float delay)
    {
        autoHideDelay = delay;
    }
    
    // Set the prompt text (which can be adjusted during runtime)
    public void SetHintText(string newHintText)
    {
        hintText = newHintText;
    }
    
    // When an object is disabled, make sure to hide the prompt.
    private void OnDisable()
    {
        HideHint();
    }
}