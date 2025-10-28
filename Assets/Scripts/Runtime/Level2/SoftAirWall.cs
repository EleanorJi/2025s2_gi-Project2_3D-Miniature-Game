using UnityEngine;
using TMPro;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class SoftAirWall : MonoBehaviour
{
    [Header("Who gets blocked")]
    public string playerTag = "Player";

    [Header("Push-back settings")]
    [Tooltip("Horizontal outward push from the wall's center to the player")]
    public float pushBackStrength = 18f;
    [Tooltip("Max horizontal speed per frame (so you don't yeet the player)")]
    public float maxHorizontalSpeed = 6f;
    [Tooltip("Extra damping so speed drops quickly when entering the zone")]
    public float damping = 12f;

    [Header("Hint UI")]
    public CanvasGroup hintGroup;   // Drag your hint panel here
    public TMP_Text hintText;
    [TextArea] public string message = "Don't get any closer... I'll be knocked back by the spray";
    public float fade = 0.2f;
    public float stay = 1.5f;

    Rigidbody _rb;
    Coroutine _uiCo;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        _rb = other.attachedRigidbody ? other.attachedRigidbody : other.GetComponent<Rigidbody>();
        if (_rb == null) return;

        ShowHintOnce();
    }

    void OnTriggerStay(Collider other)
    {
        if (_rb == null || !other.CompareTag(playerTag)) return;

        // Horizontal push-back: direction from wall center -> player
        Vector3 center = GetComponent<Collider>().bounds.center;
        Vector3 toPlayer = other.transform.position - center;
        toPlayer.y = 0f; // only worry about horizontal
        if (toPlayer.sqrMagnitude < 0.0001f) return;

        Vector3 dir = toPlayer.normalized;

        // Apply damping first to stop brute-forcing through
        Vector3 v = _rb.linearVelocity;
        Vector3 hv = new Vector3(v.x, 0f, v.z);
        hv = Vector3.Lerp(hv, Vector3.zero, 1f - Mathf.Exp(-damping * Time.deltaTime));

        // Then add an outward push
        hv += dir * (pushBackStrength * Time.deltaTime);

        // Cap horizontal speed
        if (hv.magnitude > maxHorizontalSpeed) hv = hv.normalized * maxHorizontalSpeed;

        _rb.linearVelocity = new Vector3(hv.x, v.y, hv.z);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        _rb = null;
    }

    void ShowHintOnce()
    {
        if (!hintGroup) return;
        if (_uiCo != null) StopCoroutine(_uiCo);
        _uiCo = StartCoroutine(HintRoutine());
    }

    IEnumerator HintRoutine()
    {
        if (hintText) hintText.text = message;

        // Fade in
        for (float t=0; t<fade; t+=Time.unscaledDeltaTime)
        {
            hintGroup.alpha = Mathf.Lerp(0f, 1f, t/fade);
            yield return null;
        }
        hintGroup.alpha = 1f;

        // Hold
        yield return new WaitForSecondsRealtime(stay);

        // Fade out
        for (float t=0; t<fade; t+=Time.unscaledDeltaTime)
        {
            hintGroup.alpha = Mathf.Lerp(1f, 0f, t/fade);
            yield return null;
        }
        hintGroup.alpha = 0f;
        _uiCo = null;
    }
}
