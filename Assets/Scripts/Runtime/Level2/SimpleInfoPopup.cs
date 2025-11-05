using System.Collections;
using TMPro;
using UnityEngine;

public class SimpleInfoPopup : MonoBehaviour
{
    public static SimpleInfoPopup I; // Simple singleton (can also not cross-scene, use current scene's)

    [Header("UI Refs")]
    public CanvasGroup root;            // Add CanvasGroup to Canvas root for easy show/hide
    public TextMeshProUGUI bodyText;    // Display only one sentence
    public AudioSource typingAudio;     // Looping typing sound effect (Loop=✔)

    [Header("Typing")]
    public float secondsPerChar = 0.03f;

    float _prevTimeScale = 1f;
    bool _busy;

    void Awake()
    {
        I = this;
        HideImmediate();
        // Make this Audio unaffected when "pause global sound" (we want to continue playing typing sound during pause)
        if (typingAudio) typingAudio.ignoreListenerPause = true;
    }

    public void ShowOnce(string text)
    {
        if (_busy) return;
        gameObject.SetActive(true);
        _busy = true;
        bodyText.text = "";
        root.blocksRaycasts = true;

        // —— Pause global —— //
        _prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        AudioListener.pause = true; // Pause other sounds in scene (optional)
        // Our typingAudio will continue, because ignoreListenerPause=true

        // Show UI
        root.alpha = 1f;

        // Start typing
        if (typingAudio) typingAudio.Play();
        StartCoroutine(TypeRoutine(text));
    }

    IEnumerator TypeRoutine(string full)
    {
        float spc = Mathf.Max(0.001f, secondsPerChar);
        foreach (char c in full)
        {
            bodyText.text += c;
            // Use time unaffected by timeScale
            float t = 0f;
            while (t < spc)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        // Finished typing: stop sound effect → directly close and resume game
        if (typingAudio) typingAudio.Stop();
        CloseAndResume();
    }

    void CloseAndResume()
    {
        // Hide UI
        root.alpha = 0f;
        root.blocksRaycasts = false;
        _busy = false;
        gameObject.SetActive(false);

        // —— Resume global —— //
        AudioListener.pause = false;
        Time.timeScale = _prevTimeScale;
    }

    public void HideImmediate()
    {
        if (!root) return;
        root.alpha = 0f;
        root.blocksRaycasts = false;
        gameObject.SetActive(false);
    }
}
