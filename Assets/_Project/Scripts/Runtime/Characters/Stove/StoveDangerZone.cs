using UnityEngine;
using Antventure.UI;

public class StoveDangerZone : MonoBehaviour
{
    [Header("Related Fires")]
    public FireController[] associatedFires;

    [Header("Dangerous Settings")]
    public bool isDangerous = true;
    
    [Header("Status tracking")]
    private bool wasDangerousLastCheck = true; // The default assumption is that it is initially dangerous.

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
        
        // Display UI prompt
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowStoveSafeHint();
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
        
        // Display UI prompt
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowStoveSafeHint();
        }
    }

    public void OnPlayerEnter(PlayerController player)
    {
        // check the current status
        bool isDangerousNow = IsCurrentlyDangerous();
        
        if (isDangerousNow)
        {
            Debug.Log("The player encounters a dangerous stove!");
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