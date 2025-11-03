using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ThirdCoockie : MonoBehaviour
{
    public int amount = 1;

    [Header("SFX (Optional)")]
    public bool playSfx = true;              // whether to play pickup sound
    public bool sfxAs2D = true;              // 2D=not affected by space; 3D=play at world position
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Tooltip("delay before deactivating object (seconds). 0 is OK; giving 0.02~0.05 can make 3D one-shot more stable.")]
    public float deactivateDelay = 0f;

    bool _consumed = false;                  // prevent multiple triggers

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;                // as pickup trigger
    }

    // key: when SetActive(true) again, allow pickup again
    void OnEnable()
    {
        _consumed = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_consumed) return;
        if (!other.CompareTag("Player")) return;

        _consumed = true; // lock once

        // 1��first add to inventory & refresh UI
        CookiesInventory.Instance?.Add(amount);
        if (CookiesInventory.Instance != null)
            Level3CookieUI.Instance?.Refresh(CookiesInventory.Instance.cookies);

        // 2��play sound effect (doesn't depend on this object's active state)
        if (playSfx)
            GlobalSfx.PlayCookieSfx(transform.position, sfxVolume, sfxAs2D);

        // 3��finally deactivate object
        if (deactivateDelay <= 0f) gameObject.SetActive(false);
        else StartCoroutine(DeactivateLater());
    }

    System.Collections.IEnumerator DeactivateLater()
    {
        yield return new WaitForSeconds(deactivateDelay);
        gameObject.SetActive(false);
    }
}
