using System.Collections;
using TMPro;
using UnityEngine;

public class SimpleInfoPopup : MonoBehaviour
{
    public static SimpleInfoPopup I; // 简单单例（也可不跨场景，用当前场景里的）

    [Header("UI Refs")]
    public CanvasGroup root;            // 给 Canvas 根加个 CanvasGroup 方便显隐
    public TextMeshProUGUI bodyText;    // 只显示一句话
    public AudioSource typingAudio;     // 循环打字音效（Loop=✔）

    [Header("Typing")]
    public float secondsPerChar = 0.03f;

    float _prevTimeScale = 1f;
    bool _busy;

    void Awake()
    {
        I = this;
        HideImmediate();
        // 让“暂停全局声音”时，本Audio不受影响（我们要在暂停里继续播放打字音）
        if (typingAudio) typingAudio.ignoreListenerPause = true;
    }

    public void ShowOnce(string text)
    {
        if (_busy) return;
        gameObject.SetActive(true);
        _busy = true;
        bodyText.text = "";
        root.blocksRaycasts = true;

        // —— 暂停全局 —— //
        _prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        AudioListener.pause = true; // 暂停场景里其它声音（可选）
        // 我们的 typingAudio 会继续，因为 ignoreListenerPause=true

        // 显示 UI
        root.alpha = 1f;

        // 开始打字
        if (typingAudio) typingAudio.Play();
        StartCoroutine(TypeRoutine(text));
    }

    IEnumerator TypeRoutine(string full)
    {
        float spc = Mathf.Max(0.001f, secondsPerChar);
        foreach (char c in full)
        {
            bodyText.text += c;
            // 用不受 timeScale 影响的时间
            float t = 0f;
            while (t < spc)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        // 打完：停音效 → 直接关闭并恢复游戏
        if (typingAudio) typingAudio.Stop();
        CloseAndResume();
    }

    void CloseAndResume()
    {
        // 隐藏 UI
        root.alpha = 0f;
        root.blocksRaycasts = false;
        _busy = false;
        gameObject.SetActive(false);

        // —— 恢复全局 —— //
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
