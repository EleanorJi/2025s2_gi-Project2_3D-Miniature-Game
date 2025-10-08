using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.Audio;

public class CheckpointUp : MonoBehaviour
{
    [Header("玩家对象")]
    public GameObject playerObject;           
    public GameObject antsAnimationObject;    
    public string antsAnimationTrigger = "Activate";

    [Header("玩家位置设置")]
    public Transform playerFinalPosition;     

    // ============= 仅一个固定机位（世界姿态） =============
    [Header("相机：固定机位（世界姿态）")]
    [Tooltip("把一个空物体放到你想看的机位，这里只需要这一个 Transform")]
    public Transform cameraFixedPose;
    [Tooltip("进入固定机位的时间")]
    public float cameraMoveInTime = 1.0f;

    [Header("相机：FOV（可选）")]
    public bool lockFOVDuringCinematic = true;
    public float fixedFOV = 55f;

    // ============= 从固定机位 → 跟随 的“交接” =============
    [Header("相机：从固定机位切回跟随")]
    [Tooltip("可选：手动指定一个交接位（世界姿态）。相机会先从固定机位丝滑到这里，再交还给 CameraFollow。留空则直接交还给跟随。")]
    public Transform followHandoffPose; // 可选
    public float handoffLerpTime = 0.8f;
    public bool restoreFOVAfter = true;
    public float followResumeFOV = 60f;

    [Header("动画设置")]
    public float animationDuration = 3f;

    [Header("攀爬前置条件（饼干）")]
    public int requiredCookies = 3;
    public bool consumeOnClimb = true;
    public FloodSequence flood;

    [Header("提示UI（不足时）")]
    public CanvasGroup hintGroup;
    public TMP_Text hintText;
    public string notEnoughText = "能量不足（需要 3 个饼干碎屑）";
    public float hintFadeTime = 0.2f;
    public float hintStayTime = 1.2f;

    // ==== SFX：固定机位开始音（播完一次，不被中途打断） ====
    [Header("SFX：固定机位开始音（播完一次）")]
    public AudioClip cinematicStartSfx;
    [Range(0f,1f)] public float cinematicStartVolume = 1f;
    [Tooltip("2D=不受空间影响；3D=按世界位置渲染（会用 cameraFixedPose 位置）")]
    public bool cinematicStartAs2D = true;
    public AudioMixerGroup sfxOutput;      // 可选：挂到你的 SFX Mixer 组

    [Header("SFX：淡入/自然结尾淡出")]
    public bool sfxUseFade = true;
    public float sfxFadeInTime  = 0.25f;   // 开头淡入
    public bool sfxFadeOutAtEnd = true;    // 自然结束前做尾淡出
    public float sfxEndFadeTime = 0.25f;   // 尾淡出时长

    [Header("SFX：3D参数（仅当为3D时生效）")]
    public float sfxSpatialMinDistance = 1f;
    public float sfxSpatialMaxDistance = 30f;

    private AudioSource _sfx;                  // 本地音源
    private Coroutine _fadeCo;                 // 音量淡协程
    private Coroutine _playOnceCo;             // 播放一次的主协程
    // =====================================================

    private bool playerInRange = false;
    private bool isAnimating = false;

    private PlayerInputController playerInputController;
    private PlayerController playerController;
    private CameraFollow cameraFollow;
    private Animator antsAnimator;
    private Coroutine hintCo;

    void Start()
    {
        // 找玩家
        playerObject = playerObject ? playerObject : GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerInputController = playerObject.GetComponent<PlayerInputController>();
            playerController = playerObject.GetComponent<PlayerController>();
        }

        // 找相机
        cameraFollow = Camera.main ? Camera.main.GetComponent<CameraFollow>() : null;

        // 动画替身
        if (antsAnimationObject != null)
        {
            antsAnimator = antsAnimationObject.GetComponent<Animator>();
            antsAnimationObject.SetActive(false);
        }

        if (hintGroup) hintGroup.alpha = 0f;

        // ==== SFX：初始化音源 ====
        _sfx = gameObject.GetComponent<AudioSource>();
        if (!_sfx) _sfx = gameObject.AddComponent<AudioSource>();
        _sfx.playOnAwake = false;
        _sfx.loop = false;
        _sfx.spatialBlend = cinematicStartAs2D ? 0f : 1f;
        _sfx.minDistance = Mathf.Max(0.01f, sfxSpatialMinDistance);
        _sfx.maxDistance = Mathf.Max(_sfx.minDistance + 0.01f, sfxSpatialMaxDistance);
        if (sfxOutput) _sfx.outputAudioMixerGroup = sfxOutput;
        _sfx.volume = 0f; // 为了淡入，初始0
        // ==================================
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E) && !isAnimating)
        {
            TryActivate();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isAnimating)
        {
            playerInRange = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && !isAnimating)
        {
            playerInRange = false;
            HideHintImmediate();
        }
    }

    // —— 饼干判定 —— //
    void TryActivate()
    {
        var inv = CookiesInventory.Instance;
        int have = inv ? inv.cookies : 0;

        if (inv == null)
        {
            Debug.LogWarning("[CheckpointUp] CookiesInventory.Instance 为空，跳过判定（调试中视为通过）");
            ActivateCheckpoint();
            return;
        }

        if (have < requiredCookies)
        {
            ShowHint(notEnoughText);
            return;
        }

        if (consumeOnClimb)
        {
            bool ok = inv.Spend(requiredCookies);
            if (!ok) { ShowHint(notEnoughText); return; }
        }

        ActivateCheckpoint();
    }

    // —— 主流程 —— //
    void ActivateCheckpoint()
    {
        if (isAnimating) return;
        if (!cameraFixedPose)
        {
            Debug.LogWarning("[CheckpointUp] 未设置 cameraFixedPose（固定机位），将跳过相机固定。");
        }

        isAnimating = true;

        if (playerInputController) playerInputController.DisableInput();
        if (playerController)      playerController.enabled = false;

        // 交出相机跟随权（我们要自己控制）
        if (cameraFollow) cameraFollow.SetCameraControl(false);

        if (antsAnimationObject) antsAnimationObject.SetActive(false);
        if (playerObject)        playerObject.SetActive(true);

        // ==== SFX：开始播放（整段播完；不随过场结束而打断） ====
        if (cinematicStartSfx)
        {
            PlayCinematicStartOnce();
        }
        // =====================================

        StartCoroutine(CheckpointSequence());
    }

    IEnumerator CheckpointSequence()
    {
        var cam = Camera.main;

        // 1) 从当前相机 → 固定机位（并切到固定FOV）
        if (cam && cameraFixedPose)
        {
            float startFov = cam.fieldOfView;
            float endFov   = (lockFOVDuringCinematic ? fixedFOV : startFov);
            yield return LerpCamera(cam,
                                    cam.transform.position, cam.transform.rotation, startFov,
                                    cameraFixedPose.position, cameraFixedPose.rotation, endFov,
                                    cameraMoveInTime);
        }

        // 2) 立刻把玩家传送到“新的落点”，避免回弹
        if (playerObject && playerFinalPosition)
        {
            playerObject.transform.SetPositionAndRotation(
                playerFinalPosition.position, playerFinalPosition.rotation);
        }

        // 3) 播放替身动画（此时相机固定不动）
        if (antsAnimationObject)
        {
            antsAnimationObject.SetActive(true);
            if (antsAnimator && !string.IsNullOrEmpty(antsAnimationTrigger))
                antsAnimator.SetTrigger(antsAnimationTrigger);
        }
        if (playerObject) playerObject.SetActive(false); // 用替身表演

        yield return new WaitForSeconds(animationDuration);

        // 4) 替身退场，玩家出现（已在新位置）
        if (antsAnimationObject) antsAnimationObject.SetActive(false);
        if (playerObject)        playerObject.SetActive(true);

        // 5) 从固定机位 → 跟随（有交接位/无交接位）
        if (cam && followHandoffPose)
        {
            float startFov = cam.fieldOfView;
            float endFov   = restoreFOVAfter ? followResumeFOV : startFov;

            yield return LerpCamera(cam,
                                    cam.transform.position, cam.transform.rotation, startFov,
                                    followHandoffPose.position, followHandoffPose.rotation, endFov,
                                    handoffLerpTime);

            HandBackFollow();
        }
        else
        {
            if (cam && restoreFOVAfter) cam.fieldOfView = followResumeFOV;
            HandBackFollow();
        }

        // 注意：此处不再 Stop SFX（让它自然播完）
        if (flood) flood.OnClimbSucceeded(resetCrumbs: false);

        isAnimating = false;
        playerInRange = false;
        HideHintImmediate();
    }

    void HandBackFollow()
    {
        if (cameraFollow)
        {
            try {
                var adopt = cameraFollow.GetType().GetMethod("AdoptFromCurrentCamera",
                         System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (adopt != null) adopt.Invoke(cameraFollow, null);
            } catch {}
            cameraFollow.SetCameraControl(true);
        }
        if (playerInputController) playerInputController.EnableInput();
        if (playerController)      playerController.enabled = true;
    }

    // —— 相机插值（位置/旋转/FOV 同时插）——
    IEnumerator LerpCamera(Camera cam,
                           Vector3 fromPos, Quaternion fromRot, float fromFov,
                           Vector3 toPos,   Quaternion toRot,   float toFov,
                           float duration)
    {
        if (!cam || duration <= 0f)
        {
            if (cam) { cam.transform.SetPositionAndRotation(toPos, toRot); cam.fieldOfView = toFov; }
            yield break;
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));

            cam.transform.position = Vector3.Lerp(fromPos, toPos, e);
            cam.transform.rotation = Quaternion.Slerp(fromRot, toRot, e);
            cam.fieldOfView = Mathf.Lerp(fromFov, toFov, e);

            yield return null;
        }

        cam.transform.SetPositionAndRotation(toPos, toRot);
        cam.fieldOfView = toFov;
    }

    // ==== SFX：整段播完（可淡入，尾部可淡出），不被外部打断 ====
    void PlayCinematicStartOnce()
    {
        if (!_sfx || !cinematicStartSfx) return;

        // 3D 时把音源放到固定机位位置（更贴画面）
        if (!cinematicStartAs2D && cameraFixedPose)
            _sfx.transform.position = cameraFixedPose.position;

        _sfx.spatialBlend = cinematicStartAs2D ? 0f : 1f;
        _sfx.clip = cinematicStartSfx;
        _sfx.loop = false;

        // 停掉正在进行的淡与播放
        if (_fadeCo != null) { StopCoroutine(_fadeCo); _fadeCo = null; }
        if (_playOnceCo != null) { StopCoroutine(_playOnceCo); _playOnceCo = null; }

        _playOnceCo = StartCoroutine(CoPlayOnceWithFades());
    }

    IEnumerator CoPlayOnceWithFades()
    {
        _sfx.Stop();
        _sfx.volume = sfxUseFade ? 0f : Mathf.Clamp01(cinematicStartVolume);
        _sfx.Play();

        // 开头淡入
        if (sfxUseFade && sfxFadeInTime > 0f)
        {
            yield return FadeVolume(_sfx.volume, Mathf.Clamp01(cinematicStartVolume), sfxFadeInTime);
        }
        else
        {
            _sfx.volume = Mathf.Clamp01(cinematicStartVolume);
        }

        // 计算尾淡出开始时刻（自然长度内）
        if (sfxFadeOutAtEnd && sfxUseFade && sfxEndFadeTime > 0f)
        {
            float wait = Mathf.Max(0f, cinematicStartSfx.length - sfxEndFadeTime);
            yield return new WaitForSeconds(wait);
            // 尾淡出
            yield return FadeVolume(_sfx.volume, 0f, sfxEndFadeTime);
            _sfx.Stop();
        }
        else
        {
            // 不做尾淡出：就让它自然播完
            yield return new WaitForSeconds(cinematicStartSfx.length - (_sfx.isPlaying ? _sfx.time : 0f));
            // 不调用 Stop()，让尾巴自然结束即可
        }

        _playOnceCo = null;
    }

    IEnumerator FadeVolume(float from, float to, float time)
    {
        if (!_sfx) yield break;
        if (time <= 0f) { _sfx.volume = to; yield break; }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / time;
            _sfx.volume = Mathf.Lerp(from, to, t);
            yield return null;
        }
        _sfx.volume = to;
    }
    // =================================

    // —— 简单提示 —— //
    void ShowHint(string msg)
    {
        if (!hintGroup || !hintText) { Debug.Log(msg); return; }
        if (hintCo != null) StopCoroutine(hintCo);
        hintCo = StartCoroutine(HintRoutine(msg));
    }
    void HideHintImmediate()
    {
        if (!hintGroup) return;
        if (hintCo != null) StopCoroutine(hintCo);
        hintGroup.alpha = 0f;
    }
    IEnumerator HintRoutine(string msg)
    {
        hintText.text = msg;
        for (float t=0; t<hintFadeTime; t+=Time.unscaledDeltaTime)
        { hintGroup.alpha = Mathf.Lerp(0,1,t/hintFadeTime); yield return null; }
        hintGroup.alpha = 1f;
        yield return new WaitForSecondsRealtime(hintStayTime);
        for (float t=0; t<hintFadeTime; t+=Time.unscaledDeltaTime)
        { hintGroup.alpha = Mathf.Lerp(1,0,t/hintFadeTime); yield return null; }
        hintGroup.alpha = 0f;
    }
}
