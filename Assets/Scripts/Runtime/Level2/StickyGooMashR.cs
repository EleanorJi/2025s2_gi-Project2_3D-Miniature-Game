using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class StickyGooMashR : MonoBehaviour
{
    [Header("Rule")]
    public int requiredPresses = 15;
    public float timeLimit = 8f;

    [Header("The Nth time entering the honey resulted in instant death")]
    public int instantDeathOnNth = 3;

    [Header("Counting Range")]
    public bool useGlobalCounter = true;

    [Header("UI")]
    public CanvasGroup ringGroup;
    public Image ringFill;   // Filled/Radial360
    public Image timerFill;  // Filled/Vertical Origin=Top
    public TMP_Text tipText;

    [Header("Successful Self-Rescue: Returning to the Previous Stone")]
    public float upOffset = 0.8f;
    public Transform smallLevelStart = null; // leave null: on fail/Nth time we call pc.Die()

    [Header("Key")]
    public KeyCode mashKey = KeyCode.Space;

    [Header("Freeze")]
    public bool setKinematicWhileStuck = true;
    public bool zeroVelocityWhileStuck = true;

    [Header("Death UI")]
    [TextArea]
    public string deathMessage = "Trapped by honey, failed to break free...";
    public Sprite deathSprite;
    public float deathDuration = 4f; // <=0 uses template default

    // === NEW: single SFX per mash press ===
    [Header("Sound effect (release button, plays once per press)")]
    public AudioClip mashPressSfx;
    [Range(0f, 1f)] public float mashPressVolume = 1f;
    [Tooltip("Leave blank to automatically create an AudioSource on this object")]
    public AudioSource mashAudioSource;

    // —— Internals ——
    bool busy;
    PlayerController pc;
    RockTracker tracker;
    Rigidbody rb;
    float cachedSpeed;
    bool cachedKinematic;

    int localTimes = 0;
    static int globalTimes = 0;

    void Reset() { GetComponent<Collider>().isTrigger = true; }
    void Awake()
    {
        ShowUI(false);

        // Prep the audio source (no loop, don't auto-play)
        if (!mashAudioSource)
        {
            mashAudioSource = gameObject.AddComponent<AudioSource>();
            mashAudioSource.playOnAwake = false;
            mashAudioSource.loop = false;
            mashAudioSource.spatialBlend = 0f; // UI beeps are usually 2D; set to 1 for 3D if you want
        }
    }
    void OnEnable(){ ShowUI(false); }

    // External code (e.g., respawn trigger) can call these
    public static void ResetGlobalHoneyCounter() { globalTimes = 0; }
    public void ResetLocalHoneyCounter() { localTimes = 0; }

    int  Cnt()      => useGlobalCounter ? globalTimes : localTimes;
    void SetCnt(int v){ if (useGlobalCounter) globalTimes = v; else localTimes = v; }
    int  Inc()      { int v = Cnt() + 1; SetCnt(v); return v; }

    void OnTriggerEnter(Collider other)
    {
        if (busy) return;

        var root = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform;
        if (!root.CompareTag("Player")) return;

        pc      = root.GetComponent<PlayerController>();
        tracker = root.GetComponent<RockTracker>();
        rb      = root.GetComponent<Rigidbody>();
        if (!pc || !rb) return;

        // If we don't have a "last rock" yet but we ARE on a rock now, record one
        if (tracker && tracker.lastJumpFromRock == null && tracker.currentRock != null)
            tracker.MarkJump();

        int times = Inc(); // 1,2,3...
        if (instantDeathOnNth > 0 && times >= instantDeathOnNth)
        {
            Die(true);
            return;
        }

        StartCoroutine(MashRoutine());
    }

    IEnumerator MashRoutine()
    {
        busy = true;

        // Freeze
        cachedSpeed = pc.moveSpeed; pc.moveSpeed = 0f;
        cachedKinematic = rb.isKinematic;
        if (setKinematicWhileStuck) rb.isKinematic = true;
        if (zeroVelocityWhileStuck) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }

        // UI
        if (tipText) tipText.text = $"MASH <color=red>[{mashKey}]</color> TO ESCAPE";
        if (ringFill) ringFill.fillAmount = 0f;
        if (timerFill) timerFill.fillAmount = 1f;
        ShowUI(true);

        int presses = 0; float t = timeLimit;
        while (t > 0f && presses < requiredPresses)
        {
            t -= Time.deltaTime;
            if (Input.GetKeyDown(mashKey))
            {
                presses++;
                if (ringFill) ringFill.fillAmount = (float)presses / requiredPresses;

                // NEW: play once per press, no looping
                PlayMashPressSfx();
            }
            if (timerFill) timerFill.fillAmount = Mathf.Clamp01(t / timeLimit);

            if (zeroVelocityWhileStuck && !setKinematicWhileStuck)
                rb.linearVelocity = Vector3.zero;

            yield return null;
        }

        ShowUI(false);

        if (presses >= requiredPresses) TeleportToLastRock();
        else                            Die(true);

        // Unfreeze
        if (setKinematicWhileStuck) rb.isKinematic = cachedKinematic;
        pc.moveSpeed = cachedSpeed;
        busy = false;
    }

    // Play the single "mash" SFX
    void PlayMashPressSfx()
    {
        if (!mashPressSfx || !mashAudioSource) return;

        // Use PlayOneShot so the tail of the previous sound isn't cut off
        mashAudioSource.PlayOneShot(mashPressSfx, mashPressVolume);
    }

    void TeleportToLastRock()
    {
        var rock = tracker ? tracker.lastJumpFromRock : null;
        if (rock == null) return;

        Vector3 target = rb.position;
        if (rock.respawnAnchor)
            target = rock.respawnAnchor.position + Vector3.up * 0.02f;
        else
        {
            var col = rock.GetComponentInChildren<Collider>();
            if (col) target = col.bounds.center + Vector3.up * (col.bounds.extents.y + upOffset);
        }

        bool keepK = rb.isKinematic; rb.isKinematic = true;
        rb.position = target;
        rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
        rb.isKinematic = keepK;
    }

    void Die(bool resetCounter)
    {
        ShowUI(false);

        if (smallLevelStart)
        {
            // Not a “real death” — just warp back to the small checkpoint, no death UI
            bool keepK = rb.isKinematic; rb.isKinematic = true;
            rb.position = smallLevelStart.position + Vector3.up * 0.02f;
            rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
            rb.isKinematic = keepK;
        }
        else
        {
            // Real death: run global death flow + show the death UI template
            pc.Die();
            GlobalSfx.PlayDeathSfx();

            // Only here we pop the death UI; you can customize text/sprite/duration on the component
            DeathUIOverlay.Instance?.Show(
                string.IsNullOrEmpty(deathMessage) ? null : deathMessage,
                deathSprite,
                (deathDuration > 0f) ? deathDuration : (float?)null
            );
        }

        if (resetCounter) SetCnt(0); // After death, reset counter so you get more tries
    }

    void ShowUI(bool show)
    {
        if (!ringGroup) return;
        ringGroup.alpha = show ? 1f : 0f;
        ringGroup.blocksRaycasts = false;
        ringGroup.interactable = false;
    }
}
