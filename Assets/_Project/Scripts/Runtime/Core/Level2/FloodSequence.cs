using UnityEngine;
using TMPro;
using System.Collections;

public class FloodSequence : MonoBehaviour
{
    [Header("Start Trigger")]
    public Collider startZone;               // 大区域（IsTrigger）
    public string playerTag = "Player";

    [Header("References")]
    public Transform floodWater;             // 水面（父/子都可）
    public Collider floodWaterTrigger;       // 水面触发（IsTrigger）
    public PlayerController playerController;

    [Header("Flood Settings")]
    public bool useLocalY = false;
    public float startY = -5f;
    public float targetY = 2f;
    public float duration = 60f;
    public AnimationCurve riseCurve = AnimationCurve.EaseInOut(0,0,1,1);
    public bool killWhenFull = true;

    [Header("On Water Slow")]
    [Range(0.1f, 1f)] public float slowFactor = 0.5f;

    [Header("Death UI（可选覆盖）")]
    [TextArea] public string deathMessage = "你被上涨的水淹没了…";
    public Sprite deathSprite;
    public float deathDuration = 4f;  // <=0 时使用模板默认


    [Header("Crumbs (攀爬能量)")]
    public int requiredCrumbs = 3;
    [SerializeField] int currentCrumbs = 0;
    public bool resetCrumbsOnDeath = true;

    [Tooltip("把本关掉落的饼干都放到这个父物体下；拾取时不要Destroy，只要SetActive(false)。重置会全部SetActive(true)。")]
    public Transform crumbRoot;

    [Header("Hint UI")]
    public CanvasGroup hintGroup;
    public TMP_Text hintText;
    public float hintFadeTime = 0.25f;
    public float hintStayTime = 2f;
    public int hint1RemainSec = 40; public string hint1Text = "水位上涨了，得快一点！";
    public int hint2RemainSec = 20; public string hint2Text = "不好！快被淹了，冲冲冲！";
    public int hint3RemainSec = 10; public string hint3Text = "最后几秒！再不走就完蛋！";
    [Space(4)]
    public string notEnoughEnergyText = "能量不足（需要 3 个饼干碎屑）";

    [Header("SFX")]
    public AudioSource waterLoopSfx;

    // —— 内部状态 ——
    bool _running;
    bool _playerInside;          // 我们自己“判定”的 inside 状态
    bool _slowed;
    float _originalSpeed = -1f;

    bool _hint1Shown, _hint2Shown, _hint3Shown;
    double _startTime;
    Coroutine _hintRoutine;
    Coroutine _floodRoutine;

    Transform _player;           // 玩家Transform用于几何包含判断

    void Start()
    {
        // 拿到玩家 Transform（容错）
        if (playerController) _player = playerController.transform;
        if (!_player)
        {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go) _player = go.transform;
        }

        SetWaterY(startY);
        if (hintGroup) hintGroup.alpha = 0f;

        // 水面触发：进入减速，离开恢复
        if (floodWaterTrigger)
        {
            var relay = floodWaterTrigger.GetComponent<WaterContactRelay>();
            if (!relay) relay = floodWaterTrigger.gameObject.AddComponent<WaterContactRelay>();
            relay.playerTag = playerTag;
            relay.onEnter = OnWaterEnter;
            relay.onExit  = OnWaterExit;
        }

        // 开局根据玩家是否在区内来决定是否启动
        _playerInside = IsPlayerInsideStartZone();
        if (_playerInside) BeginSequence(); else ResetSequence();
    }

    void Update()
    {
        // 每帧主动判定玩家是否在 startZone 内（解决“死亡传送/禁用不触发 OnTriggerExit”的问题）
        bool insideNow = IsPlayerInsideStartZone();

        if (insideNow && !_playerInside)
        {
            _playerInside = true;
            BeginSequence();             // 重新进入：重新开始涨水
        }
        else if (!insideNow && _playerInside)
        {
            _playerInside = false;
            // 离开（比如死亡回到上方重生点）：立刻复位并清零关内饼干
            ResetSequence();
            ResetCrumbs();
        }
    }

    bool IsPlayerInsideStartZone()
    {
        if (!startZone || !_player) return false;
        // 利用 ClosestPoint 判断：如果最近点就是自己位置，说明在触发器体积内
        Vector3 p = _player.position;
        Vector3 cp = startZone.ClosestPoint(p);
        return (cp - p).sqrMagnitude < 1e-6f;
    }

    // 统一设置水位（本地/世界）
    void SetWaterY(float y)
    {
        if (!floodWater) return;
        if (useLocalY)
        {
            var lp = floodWater.localPosition; lp.y = y; floodWater.localPosition = lp;
        }
        else
        {
            var p = floodWater.position; p.y = y; floodWater.position = p;
        }
    }

    public void BeginSequence()
    {
        if (_running) return;
        _running = true;
        _startTime = Time.timeAsDouble;
        _hint1Shown = _hint2Shown = _hint3Shown = false;

        if (waterLoopSfx) waterLoopSfx.Play();
        _floodRoutine = StartCoroutine(RunFlood());
    }

    public void ResetSequence(bool stopCompletely = false)
    {
        if (_floodRoutine != null) StopCoroutine(_floodRoutine);
        _floodRoutine = null;
        _running = false;

        SetWaterY(startY);
        HideHintImmediate();
        if (waterLoopSfx) waterLoopSfx.Stop();
        RestoreSpeedIfNeeded();

        _hint1Shown = _hint2Shown = _hint3Shown = false;
        _startTime = 0;
    }

    IEnumerator RunFlood()
    {
        while (_running)
        {
            double elapsed = Time.timeAsDouble - _startTime;
            float t01 = Mathf.Clamp01((float)(elapsed / duration));
            float k   = riseCurve.Evaluate(t01);

            SetWaterY(Mathf.Lerp(startY, targetY, k));

            int remain = Mathf.Max(0, Mathf.CeilToInt((float)(duration - elapsed)));
            if (!_hint1Shown && remain <= hint1RemainSec) { _hint1Shown = true; ShowHint(hint1Text); }
            if (!_hint2Shown && remain <= hint2RemainSec) { _hint2Shown = true; ShowHint(hint2Text); }
            if (!_hint3Shown && remain <= hint3RemainSec) { _hint3Shown = true; ShowHint(hint3Text); }

            if (t01 >= 1f)
            {
                _running = false;
                if (waterLoopSfx) waterLoopSfx.Stop();

                if (killWhenFull && playerController)
                {
                    playerController.Die();
                    GlobalSfx.PlayDeathSfx();


                    // —— 弹出全局死亡模板（文案/图片/时长可在本组件里改；为空就用模板默认）——
                    DeathUIOverlay.Instance?.Show(
                        string.IsNullOrEmpty(deathMessage) ? null : deathMessage,
                        deathSprite,
                        (deathDuration > 0f) ? deathDuration : (float?)null
                    );
                    if (resetCrumbsOnDeath) ResetCrumbs();  // ★ 被淹死：清零并复活碎屑
                }
                ResetSequence();
                yield break;
            }

            // 容错：如果外部把 _playerInside 改成 false，协程也会及时结束
            if (!_playerInside)
            {
                ResetSequence();
                yield break;
            }

            yield return null;
        }
    }

    // 触水减速
    void OnWaterEnter(Collider other)
    {
        if (playerController == null || _slowed) return;
        _originalSpeed = playerController.moveSpeed;
        playerController.moveSpeed = _originalSpeed * Mathf.Clamp01(slowFactor);
        _slowed = true;
    }
    void OnWaterExit(Collider other) => RestoreSpeedIfNeeded();

    void RestoreSpeedIfNeeded()
    {
        if (!_slowed || playerController == null) return;
        if (_originalSpeed > 0f) playerController.moveSpeed = _originalSpeed;
        _slowed = false;
        _originalSpeed = -1f;
    }

    // Hint UI
    void ShowHint(string msg)
    {
        if (!hintGroup || !hintText) return;
        if (_hintRoutine != null) StopCoroutine(_hintRoutine);
        _hintRoutine = StartCoroutine(HintRoutine(msg));
    }
    void HideHintImmediate()
    {
        if (!hintGroup) return;
        if (_hintRoutine != null) StopCoroutine(_hintRoutine);
        hintGroup.alpha = 0f;
    }
    IEnumerator HintRoutine(string msg)
    {
        hintText.text = msg;
        for (float t=0; t<hintFadeTime; t+=Time.unscaledDeltaTime)
        { hintGroup.alpha = Mathf.Lerp(0f,1f,t/hintFadeTime); yield return null; }
        hintGroup.alpha = 1f;
        yield return new WaitForSecondsRealtime(hintStayTime);
        for (float t=0; t<hintFadeTime; t+=Time.unscaledDeltaTime)
        { hintGroup.alpha = Mathf.Lerp(1f,0f,t/hintFadeTime); yield return null; }
        hintGroup.alpha = 0f;
    }

    // —— Crumbs 接口/复位 —— //
    public void AddCrumb(int amount = 1) { currentCrumbs = Mathf.Max(0, currentCrumbs + amount); }
    public int  GetCrumbCount() => currentCrumbs;
    public bool HasEnoughCrumbs() => currentCrumbs >= requiredCrumbs;

    public void OnClimbSucceeded(bool resetCrumbs = true)
    {
        ResetSequence(stopCompletely: true);
        if (resetCrumbs) ResetCrumbs();
    }

    void ResetCrumbs()
{
    // 1) 清本地计数（如果你还在用它）
    currentCrumbs = 0;

    // 2) 清真正库存并触发 UI 事件
    if (CookiesInventory.Instance != null)
        CookiesInventory.Instance.Clear();   // ★ 关键

    // 3) 复活场景里的碎屑（确保拾取改成 SetActive(false)）
    if (crumbRoot)
    {
        for (int i = 0; i < crumbRoot.childCount; i++)
            crumbRoot.GetChild(i).gameObject.SetActive(true);
    }

    // 4) 万一 UI 没监听事件，可兜底强刷一次（可留可删）
    SkillChargeUI.Instance?.Refresh(0);
}

}
