using UnityEngine;
using TMPro;
using System.Collections;

public class FloodSequence : MonoBehaviour
{
    [Header("Start Trigger")]
    public Collider startZone;                 // StartZone: BoxCollider (IsTrigger = true)
    public string playerTag = "Player";

    [Header("References")]
    public Transform floodWater;               // 水面物体 Transform
    public Collider floodWaterTrigger;         // 水面上的 BoxCollider (IsTrigger = true)
    public PlayerController playerController;  // 直接拖你的 PlayerController 组件

    [Header("Flood Settings")]
    public float startY = -5f;
    public float targetY = 2f;
    public float duration = 60f;               // 总时长（秒）
    public AnimationCurve riseCurve = AnimationCurve.EaseInOut(0,0,1,1);
    public bool killWhenFull = true;           // 涨满是否判死

    [Header("Slow On Water")]
    [Range(0.1f, 1f)] public float slowFactor = 0.5f;  // 进入水面时的速度倍率（你要“一半”就 0.5）
    private bool _slowed;                      // 是否处于减速
    private float _originalSpeed = -1f;        // 记录进入水前的原始速度

    [Header("Hint UI (只提示不显示时间)")]
    public CanvasGroup hintGroup;              // AntHintPanel 的 CanvasGroup（默认 Alpha=0）
    public TMP_Text hintText;                  // AntHintText
    public float hintFadeTime = 0.25f;
    public float hintStayTime = 2f;
    public int hint1RemainSec = 40; public string hint1Text = "水位上涨了，得快一点！";
    public int hint2RemainSec = 20; public string hint2Text = "不好！快被淹了，冲冲冲！";
    public int hint3RemainSec = 10; public string hint3Text = "最后几秒！再不走就完蛋！";

    [Header("Optional SFX")]
    public AudioSource waterLoopSfx;

    // —— 内部状态 ——
    bool _running;
    bool _hint1Shown, _hint2Shown, _hint3Shown;
    double _startTime;
    Coroutine _hintRoutine;

    void Start()
    {
        // 初始化水位
        if (floodWater)
        {
            var p = floodWater.position; p.y = startY; floodWater.position = p;
        }
        if (hintGroup) hintGroup.alpha = 0f;

        // 触发区：玩家进入后开始计时/涨水
        if (startZone)
        {
            var zs = startZone.gameObject.GetComponent<FloodStartZone>();
            if (!zs) zs = startZone.gameObject.AddComponent<FloodStartZone>();
            zs.playerTag = playerTag;
            zs.onPlayerEnter = BeginSequence;
        }

        // 水面触发：进入减速，离开恢复
        if (floodWaterTrigger)
        {
            var relay = floodWaterTrigger.gameObject.GetComponent<WaterContactRelay>();
            if (!relay) relay = floodWaterTrigger.gameObject.AddComponent<WaterContactRelay>();
            relay.playerTag = playerTag;
            relay.onEnter = OnWaterEnter;
            relay.onExit  = OnWaterExit;
        }
    }

    public void BeginSequence()
    {
        if (_running) return;
        _running = true;
        _startTime = Time.timeAsDouble;
        _hint1Shown = _hint2Shown = _hint3Shown = false;

        if (waterLoopSfx) waterLoopSfx.Play();
        StartCoroutine(RunFlood());
    }

    IEnumerator RunFlood()
    {
        while (true)
        {
            double elapsed = Time.timeAsDouble - _startTime;
            float t01 = Mathf.Clamp01((float)(elapsed / duration));
            float k   = riseCurve.Evaluate(t01);

            // 水位上升
            if (floodWater)
            {
                var p = floodWater.position;
                p.y = Mathf.Lerp(startY, targetY, k);
                floodWater.position = p;
            }

            // 内部剩余秒（不显示，只用来触发提示）
            int remain = Mathf.Max(0, Mathf.CeilToInt((float)(duration - elapsed)));

            if (!_hint1Shown && remain <= hint1RemainSec) { _hint1Shown = true; ShowHint(hint1Text); }
            if (!_hint2Shown && remain <= hint2RemainSec) { _hint2Shown = true; ShowHint(hint2Text); }
            if (!_hint3Shown && remain <= hint3RemainSec) { _hint3Shown = true; ShowHint(hint3Text); }

            if (t01 >= 1f) break; // 时间到/水位满

            yield return null;
        }

        _running = false;
        if (waterLoopSfx) waterLoopSfx.Stop();

        if (killWhenFull && playerController != null)
        {
            // 涨满：判死并回到存档点（用你现有 Die()）
            playerController.Die();
            // 同时确保恢复减速状态
            RestoreSpeedIfNeeded();
        }
    }

    // —— 触水减速 / 离开恢复 ——
    void OnWaterEnter(Collider other)
    {
        if (playerController == null) return;
        if (_slowed) return;

        // 记录原速并减速（砍为一半）
        _originalSpeed = playerController.moveSpeed;
        playerController.moveSpeed = _originalSpeed * Mathf.Clamp01(slowFactor);
        _slowed = true;
    }

    void OnWaterExit(Collider other)
    {
        RestoreSpeedIfNeeded();
    }

    void RestoreSpeedIfNeeded()
    {
        if (playerController == null) return;
        if (!_slowed) return;
        if (_originalSpeed > 0f)
            playerController.moveSpeed = _originalSpeed;
        _slowed = false;
        _originalSpeed = -1f;
    }

    // —— 三段提示（淡入/停留/淡出） ——
    void ShowHint(string msg)
    {
        if (!hintGroup || !hintText) return;
        if (_hintRoutine != null) StopCoroutine(_hintRoutine);
        _hintRoutine = StartCoroutine(HintRoutine(msg));
    }

    IEnumerator HintRoutine(string msg)
    {
        hintText.text = msg;
        // 淡入
        for (float t=0; t<hintFadeTime; t+=Time.unscaledDeltaTime)
        {
            hintGroup.alpha = Mathf.Lerp(0f, 1f, t/hintFadeTime);
            yield return null;
        }
        hintGroup.alpha = 1f;
        // 停留
        yield return new WaitForSecondsRealtime(hintStayTime);
        // 淡出
        for (float t=0; t<hintFadeTime; t+=Time.unscaledDeltaTime)
        {
            hintGroup.alpha = Mathf.Lerp(1f, 0f, t/hintFadeTime);
            yield return null;
        }
        hintGroup.alpha = 0f;
    }
}

// 进入 StartZone 开始
public class FloodStartZone : MonoBehaviour
{
    public string playerTag = "Player";
    public System.Action onPlayerEnter;
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag)) onPlayerEnter?.Invoke();
    }
}

// 水面的触发转发器
public class WaterContactRelay : MonoBehaviour
{
    public string playerTag = "Player";
    public System.Action<Collider> onEnter, onExit;
    void OnTriggerEnter(Collider other) { if (other.CompareTag(playerTag)) onEnter?.Invoke(other); }
    void OnTriggerExit (Collider other) { if (other.CompareTag(playerTag)) onExit ?.Invoke(other); }
}
