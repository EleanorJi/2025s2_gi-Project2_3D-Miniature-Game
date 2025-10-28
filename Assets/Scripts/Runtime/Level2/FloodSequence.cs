using UnityEngine;
using TMPro;
using System.Collections;

public class FloodSequence : MonoBehaviour
{
    [Header("Start Trigger")]
    public Collider startZone;
    public string playerTag = "Player";

    [Header("References")]
    public Transform floodWater;
    public Collider floodWaterTrigger;
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

    [Header("Death UI (optional override)")]
    [TextArea] public string deathMessage = "You were drowned by the rising water…";
    public Sprite deathSprite;
    public float deathDuration = 4f;  // <=0 uses the default from the template

    [Header("Crumbs (climb energy)")]
    public int requiredCrumbs = 3;
    [SerializeField] int currentCrumbs = 0;
    public bool resetCrumbsOnDeath = true;

    [Tooltip("Put all cookies dropped in THIS level under this parent; when picked up, don't Destroy — just SetActive(false). Reset will SetActive(true) on all of them.")]
    public Transform crumbRoot;

    [Header("Hint UI")]
    public CanvasGroup hintGroup;
    public TMP_Text hintText;
    public float hintFadeTime = 0.25f;
    public float hintStayTime = 2f;
    public int hint1RemainSec = 40; public string hint1Text = "Water is rising — move faster!";
    public int hint2RemainSec = 20; public string hint2Text = "Uh-oh! Almost flooded. Go go go!";
    public int hint3RemainSec = 10; public string hint3Text = "Last seconds! Move or lose!";
    [Space(4)]
    public string notEnoughEnergyText = "Not enough energy (need 3 cookie crumbs)";

    [Header("SFX")]
    public AudioSource waterLoopSfx;

    // ───── Exit-area clearing policy (keep previous behavior) ─────
    [Header("Crumbs clearing policy (on leaving the area)")]
    [Tooltip("Always = always clear; OnlyIfDiedInside = clear only if a death happened inside during this run; Never = never clear")]
    public ExitClearMode exitClearMode = ExitClearMode.OnlyIfDiedInside;
    public enum ExitClearMode { Always, OnlyIfDiedInside, Never }

    // ───── Respawn point (added / restored) ─────
    [Header("Respawn (die inside -> respawn)")]
    public Transform respawnPoint;           // Place this respawn point outside Level 1
    public bool warpOnDeath = true;          // If checked: instantly warp to respawn on death inside

    // —— Internal state ——
    bool _running;
    bool _playerInside;
    bool _slowed;
    float _originalSpeed = -1f;

    bool _hint1Shown, _hint2Shown, _hint3Shown;
    double _startTime;
    Coroutine _hintRoutine;
    Coroutine _floodRoutine;

    Transform _player;

    // —— Flags + counter for deaths during THIS run (enter area -> leave area) ——
    bool _diedInsideSinceEnter = false;
    public int deathCountThisRun { get; private set; } = 0;

    void Start()
    {
        // Grab player's Transform (be forgiving)
        if (playerController) _player = playerController.transform;
        if (!_player)
        {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go) _player = go.transform;
        }

        SetWaterY(startY);
        if (hintGroup) hintGroup.alpha = 0f;

        // Water contact trigger: enter to slow, exit to restore
        if (floodWaterTrigger)
        {
            var relay = floodWaterTrigger.GetComponent<WaterContactRelay>();
            if (!relay) relay = floodWaterTrigger.gameObject.AddComponent<WaterContactRelay>();
            relay.playerTag = playerTag;
            relay.onEnter = OnWaterEnter;
            relay.onExit  = OnWaterExit;
        }

        // On start, decide whether to run based on whether player is already inside
        _playerInside = IsPlayerInsideStartZone();
        if (_playerInside) BeginSequence(); else ResetSequence();
    }

    void Update()
    {
        // Actively check if the player is inside startZone
        bool insideNow = IsPlayerInsideStartZone();

        if (insideNow && !_playerInside)
        {
            _playerInside = true;
            BeginSequence(); // re-enter: start flood & clear per-run death counter
        }
        else if (!insideNow && _playerInside)
        {
            _playerInside = false;

            // Left the area: reset water, hints, etc.
            ResetSequence();

            // Decide whether to clear global crumbs/UI
            bool shouldClearGlobal = (exitClearMode == ExitClearMode.Always)
                                   || (exitClearMode == ExitClearMode.OnlyIfDiedInside && _diedInsideSinceEnter);

            if (shouldClearGlobal)
            {
                ResetCrumbs();                  // Clear global (UI to zero + respawn crumbs)
                deathCountThisRun = 0;          // Next time we enter, start from 0 again
            }
            else
            {
                ResetLevelLocalCrumbsOnly();    // Only restore level drops, keep global/UI
                // If we passed the gate successfully -> keep die=0 into next level
            }
        }
    }

    bool IsPlayerInsideStartZone()
    {
        if (!startZone || !_player) return false;
        Vector3 p = _player.position;
        Vector3 cp = startZone.ClosestPoint(p);
        return (cp - p).sqrMagnitude < 1e-6f;
    }

    // Set water Y either in local or world space
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

        // New run: clear death flags/counter
        _diedInsideSinceEnter = false;
        deathCountThisRun = 0;

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
    // 公开个只读接口，外部能判断“此刻是否在本水域内”
    public bool IsPlayerInsideZonePublic() => IsPlayerInsideStartZone();
    
    // 供外部（蜘蛛、电击等）调用：把这次死亡当作“死在水域内”来收尾
    public void HandleDeathInsideExternal()
    {
        if (!IsPlayerInsideStartZone()) return;   // 只在确实在本区域内时生效
    
        _diedInsideSinceEnter = true;
        deathCountThisRun++;
    
        if (resetCrumbsOnDeath) ResetCrumbs();    // 清零全局饼干 + 恢复关内掉落
        if (warpOnDeath) WarpPlayerToRespawn();   // 立刻回重生点（与你水满时的逻辑一致）
    
        ResetSequence();                          // 停止涨水、收起提示、复位水位/音效
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
                    // Killed by water inside the area: record death
                    _diedInsideSinceEnter = true;
                    deathCountThisRun++;

                    playerController.Die();
                    GlobalSfx.PlayDeathSfx();

                    // Death UI
                    DeathUIOverlay.Instance?.Show(
                        string.IsNullOrEmpty(deathMessage) ? null : deathMessage,
                        deathSprite,
                        (deathDuration > 0f) ? deathDuration : (float?)null
                    );

                    // Death inside area: per your design, clear global cookies and restore drops
                    if (resetCrumbsOnDeath) ResetCrumbs();

                    // ★ Instantly warp to respawn (don’t wait for “left area” to trigger)
                    if (warpOnDeath) WarpPlayerToRespawn();
                }

                ResetSequence();
                yield break;
            }

            // Safety: if something flipped _playerInside to false externally, stop the coroutine
            if (!_playerInside)
            {
                ResetSequence();
                yield break;
            }

            yield return null;
        }
    }

    // Touching water slows you down
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

    // —— Crumbs API / reset —— //
    public void AddCrumb(int amount = 1) { currentCrumbs = Mathf.Max(0, currentCrumbs + amount); }
    public int  GetCrumbCount() => currentCrumbs;
    public bool HasEnoughCrumbs() => currentCrumbs >= requiredCrumbs;

    public void OnClimbSucceeded(bool resetCrumbs = true)
    {
        ResetSequence(stopCompletely: true);
        if (resetCrumbs) ResetCrumbs();
    }

    // Only restore level-dropped cookies, don’t clear global stock/UI (used when leaving safely)
    void ResetLevelLocalCrumbsOnly()
    {
        currentCrumbs = 0;
        if (crumbRoot)
        {
            for (int i = 0; i < crumbRoot.childCount; i++)
                crumbRoot.GetChild(i).gameObject.SetActive(true);
        }
    }

    // Clear global + restore dropped cookies + refresh UI (used on death inside or by policy)
    void ResetCrumbs()
    {
        currentCrumbs = 0;
    
        if (CookiesInventory.Instance != null)
            CookiesInventory.Instance.Clear();   // Triggers UI to zero
    
        //Recursive restore: no matter how deep the nesting, reactivate all cookie pickups
        if (crumbRoot)
        {
            var allCrumbs = crumbRoot.GetComponentsInChildren<CookiePickup>(true);
            foreach (var c in allCrumbs)
                c.gameObject.SetActive(true);
        }
    
        SkillChargeUI.Instance?.Refresh(0);
    }


    //Call this when player dies due to other causes inside the zone: mark “died this run”
    public void NotifyPlayerDiedInside()
    {
        if (IsPlayerInsideStartZone())
        {
            _diedInsideSinceEnter = true;
            deathCountThisRun++;
        }
    }

    //Actually warp to respawn (with some safety for Rigidbody / CharacterController)
    public void WarpPlayerToRespawn()
    {
        if (!respawnPoint || !_player) return;

        // Try grabbing rigidbody / character controller
        var rb = _player.GetComponent<Rigidbody>();
        var cc = _player.GetComponent<CharacterController>();
        var agent = _player.GetComponent<UnityEngine.AI.NavMeshAgent>();

        bool ccWas = false; bool agentWas = false;

        if (cc) { ccWas = cc.enabled; cc.enabled = false; }
        if (agent){ agentWas = agent.enabled; agent.enabled = false; }

        if (rb)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        _player.SetPositionAndRotation(respawnPoint.position, respawnPoint.rotation);

        if (rb)
        {
            rb.isKinematic = false;
            rb.WakeUp();
        }
        if (cc) cc.enabled = ccWas;
        if (agent) agent.enabled = agentWas;

        Physics.SyncTransforms();
    }
}
