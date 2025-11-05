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
        Debug.Log($"[StoveDangerZone] OnPlayerEnter called! Player: {player != null}");
        
        // check the current status
        bool isDangerousNow = IsCurrentlyDangerous();
        Debug.Log($"[StoveDangerZone] Is dangerous: {isDangerousNow}");
        
        if (isDangerousNow)
        {
            Debug.Log("[StoveDangerZone] The player encounters a dangerous stove!");

            // Play the death sound effect
            GlobalSfx.PlayDeathSfx();

            // Set custom death UI message and sprite before triggering death
            // This will be used by DeathUI_AutoWire when it handles the death event
            float displayDuration = deathHoldSeconds > 0f ? deathHoldSeconds : 4f;
            
            Debug.Log($"[StoveDangerZone] Setting custom death UI - Message: '{deathMessage}', Sprite: {deathSprite != null}, Sprite name: {(deathSprite != null ? deathSprite.name : "NULL")}, Duration: {displayDuration}");
            DeathUI_AutoWire.SetCustomDeathUI(deathMessage, deathSprite, displayDuration);
            Debug.Log("[StoveDangerZone] SetCustomDeathUI called");

            // Trigger death through PlayerHealth, which will trigger DeathUI_AutoWire.HandleDied()
            // DeathUI_AutoWire will use the custom message and sprite we just set
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            Debug.Log($"[StoveDangerZone] PlayerHealth found: {playerHealth != null}");
            
            if (playerHealth != null)
            {
                int currentHP = playerHealth.CurrentHealth;
                Debug.Log($"[StoveDangerZone] Current HP: {currentHP}, calling TakeDamage...");
                playerHealth.TakeDamage("StoveDangerZone", currentHP);
                Debug.Log("[StoveDangerZone] TakeDamage called");
            }
            else
            {
                Debug.LogWarning("[StoveDangerZone] No PlayerHealth found, using fallback");
                // Fallback: use player.Die() if no PlayerHealth
                // In this case, use DeathUIOverlay directly
                if (DeathUIOverlay.Instance != null)
                {
                    if (deathHoldSeconds > 0f)
                        DeathUIOverlay.Instance.Show(deathMessage, deathSprite, deathHoldSeconds);
                    else
                        DeathUIOverlay.Instance.Show(deathMessage, deathSprite, null);
                }
                player.Die();
            }
        }
        else
        {
            Debug.Log("[StoveDangerZone] The stove is now safe.");
        }
    }

    // Continuously check for status changes in the Update
    private void Update()
    {
        IsCurrentlyDangerous();
    }
}