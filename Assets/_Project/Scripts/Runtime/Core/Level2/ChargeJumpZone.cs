using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Attach to a trigger zone with Box/Sphere/Capsule Collider (IsTrigger ✔)
/// Function: player enters→enable ChargeJumpModule; leaves→disable ChargeJumpModule
/// Now added: pop up hint UI once when entering zone (optional, won't show if no reference)
/// </summary>
[RequireComponent(typeof(Collider))]
public class ChargeJumpZone : MonoBehaviour
{
    [Header("Filter")]
    public bool onlyAffectTag = true;
    public string requiredTag = "Player";   // only affect this Tag

    [Header("Enter/Exit Behavior")]
    public bool enableOnEnter = true;       // enter: enable charge jump
    public bool disableOnExit = true;       // exit: disable charge jump
    [Tooltip("delay before disabling after exit (seconds), 0=disable immediately")]
    public float disableDelay = 0f;

    [Header("Entry Hint UI (Optional)")]
    public CanvasGroup hintGroup;           // drag a panel (CanvasGroup), default alpha=0
    public TMP_Text hintText;               // text on panel
    [TextArea] public string enterMessage = "在这个区域不要被粘稠的垃圾汤粘住！";
    public float hintFadeIn = 0.2f;
    public float hintStay   = 2.0f;
    public float hintFadeOut= 0.2f;
    public bool  hideOnExit = true;         // immediately hide hint when exiting (optional)

    Coroutine hintCo;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (onlyAffectTag && !other.CompareTag(requiredTag)) return;

        // enable charge jump
        var mod = other.GetComponent<ChargeJumpModule>();
        if (mod && enableOnEnter) mod.SetChargeEnabled(true);

        // pop hint (optional)
        if (hintGroup && hintText)
        {
            ShowHintOnce(enterMessage);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (onlyAffectTag && !other.CompareTag(requiredTag)) return;

        // disable charge jump
        var mod = other.GetComponent<ChargeJumpModule>();
        if (mod && disableOnExit)
        {
            if (disableDelay <= 0f) mod.SetChargeEnabled(false);
            else StartCoroutine(DisableLater(mod, disableDelay));
        }

        // optionally hide hint when exiting
        if (hideOnExit) HideHintImmediate();
    }

    IEnumerator DisableLater(ChargeJumpModule mod, float t)
    {
        yield return new WaitForSeconds(t);
        if (mod) mod.SetChargeEnabled(false);
    }

    // —— Hint UI —— //
    void ShowHintOnce(string msg)
    {
        if (!hintGroup || !hintText) return;
        if (hintCo != null) StopCoroutine(hintCo);
        hintCo = StartCoroutine(HintRoutine(msg));
    }

    IEnumerator HintRoutine(string msg)
    {
        hintText.text = msg;

        // fade in
        for (float t = 0f; t < hintFadeIn; t += Time.unscaledDeltaTime)
        {
            hintGroup.alpha = Mathf.Lerp(0f, 1f, t / hintFadeIn);
            yield return null;
        }
        hintGroup.alpha = 1f;

        // stay
        yield return new WaitForSecondsRealtime(hintStay);

        // fade out
        for (float t = 0f; t < hintFadeOut; t += Time.unscaledDeltaTime)
        {
            hintGroup.alpha = Mathf.Lerp(1f, 0f, t / hintFadeOut);
            yield return null;
        }
        hintGroup.alpha = 0f;
        hintCo = null;
    }

    void HideHintImmediate()
    {
        if (!hintGroup) return;
        if (hintCo != null) StopCoroutine(hintCo);
        hintGroup.alpha = 0f;
        hintCo = null;
    }

#if UNITY_EDITOR
    // small visualization
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
        Gizmos.matrix = transform.localToWorldMatrix;
        var box = GetComponent<BoxCollider>();
        if (box && box.isTrigger) Gizmos.DrawCube(box.center, box.size);
    }
#endif
}
