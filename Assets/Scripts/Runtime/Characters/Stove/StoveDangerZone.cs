using UnityEngine;
using Antventure.UI;

public class StoveDangerZone : MonoBehaviour
{
    [Header("Related Fires")]
    public FireController[] associatedFires;

    [Header("Dangerous Settings")]
    public bool isDangerous = true;
    
    [Header("Checkpoint Reference")]
    [SerializeField] private Checkpoint checkpoint; // Reference to checkpoint
    
    [Header("Status tracking")]
    private bool wasDangerousLastCheck = true; // The default assumption is that it is initially dangerous.

    [Header("Death UI Settings (Stove Specific)")]
    [TextArea] public string deathMessage = "You have been consumed by the flames!";
    public Sprite deathSprite;
    [Tooltip("<=0 uses default duration")]
    public float deathHoldSeconds = 4f;

    void Start()
    {
        if (associatedFires == null || associatedFires.Length == 0)
        {
            associatedFires = FindObjectsByType<FireController>(FindObjectsSortMode.None);
        }
        
        // initial
        wasDangerousLastCheck = IsCurrentlyDangerousInternal();
    }

    // Internal inspection method, does not trigger a state change event
    private bool IsCurrentlyDangerousInternal()
    {
        if (!isDangerous) return false;
        
        // If any of the associated flames are still shrinking or 
        // have not completely stopped shrinking, it indicates a dangerous situation.
        foreach (FireController fire in associatedFires)
        {
            if (fire != null && (fire.IsShrinking || !fire.IsFullyShrunk))
            {
                return true;
            }
        }
        
        return false;
    }

    // The public inspection method will trigger a state change event.
    public bool IsCurrentlyDangerous()
    {
        bool isDangerousNow = IsCurrentlyDangerousInternal();
        
        // Check if the status has changed from dangerous to safe
        if (wasDangerousLastCheck && !isDangerousNow)
        {
            OnBecameSafe();
        }
        // Check if the status has changed from safe to dangerous
        else if (!wasDangerousLastCheck && isDangerousNow)
        {
            OnBecameDangerous();
        }
        
        // Update the status of the last inspection
        wasDangerousLastCheck = isDangerousNow;
        
        return isDangerousNow;
    }

    // It is called when the stove changes from being dangerous to being safe.
    private void OnBecameSafe()
    {
        Debug.Log("The stove is now safe - it has transitioned from a dangerous state to a safe state.");
        
        // Show alternative text on checkpoint permanently (replaces original hint)
        if (checkpoint != null)
        {
            Debug.Log("StoveDangerZone: Calling ShowAlternativeTextPermanently on assigned checkpoint");
            checkpoint.ShowAlternativeTextPermanently("Stove is now safe!");
        }
        else
        {
            // Fallback: try to find checkpoint if not assigned
            var foundcheckpoint = GameObject.Find("Checkpoint2")?.GetComponent<Checkpoint>();
            if (foundcheckpoint != null)
            {
                foundcheckpoint.ShowAlternativeTextPermanently("Stove is now safe!");
            }
            else
            {
                // Final fallback to original UI prompt
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowStoveSafeHint();
                }
            }
        }
    }

    // Call when the stove changes from safe to dangerous
    private void OnBecameDangerous()
    {
        Debug.Log("The stove has become dangerous!");
    }

    // Manually invoke to check for changes in the status
    public void CheckStateChange()
    {
        IsCurrentlyDangerous();
    }

    // Manually activate when the stove is safe.
    public void OnStoveSafe()
    {
        Debug.Log("stove save now!");
        
        // Show alternative text on checkpoint permanently (replaces original hint)
        if (checkpoint != null)
        {
            checkpoint.ShowAlternativeTextPermanently("Stove is now safe!");
        }
        else
        {
            // Fallback: try to find checkpoint if not assigned
            var foundcheckpoint = GameObject.Find("Checkpoint2")?.GetComponent<Checkpoint>();
            if (foundcheckpoint != null)
            {
                foundcheckpoint.ShowAlternativeTextPermanently("Stove is now safe!");
            }
            else
            {
                // Final fallback to original UI prompt
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowStoveSafeHint();
                }
            }
        }
    }

    public void OnPlayerEnter(PlayerController player)
    {
        // check the current status
        bool isDangerousNow = IsCurrentlyDangerous();
        
        if (isDangerousNow)
        {
            Debug.Log("The player encounters a dangerous stove!");

            // Play the death sound effect
            GlobalSfx.PlayDeathSfx();

            // Display the configurable death UI
            if (DeathUIOverlay.Instance != null)
            {
                if (deathHoldSeconds > 0f)
                    DeathUIOverlay.Instance.Show(deathMessage, deathSprite, deathHoldSeconds);
                else
                    DeathUIOverlay.Instance.Show(deathMessage, deathSprite, null);
            }

            // kill Player
            player.Die();
        }
        else
        {
            Debug.Log("The stove is now safe.");
        }
    }

    // Continuously check for status changes in the Update
    private void Update()
    {
        IsCurrentlyDangerous();
    }
}