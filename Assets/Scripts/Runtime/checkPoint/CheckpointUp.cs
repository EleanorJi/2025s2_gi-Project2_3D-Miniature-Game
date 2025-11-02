using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.Audio;
using System;

public class CheckpointUp : MonoBehaviour
{
    // Static event for climbing success
    public static event Action OnClimbingSuccess;
    [Header("Player Objects")]
    public GameObject playerObject;           
    public GameObject antsAnimationObject;    
    public string antsAnimationTrigger = "Activate";
    public PlayerPoisonShooter poisonShooter;
    [Header("Player Position Settings")]
    public Transform playerFinalPosition;     

    // ============= Single Fixed Camera Position (World Space) =============
    [Header("Camera: Fixed Position (World Space)")]
    [Tooltip("Put an empty object where you want the camera to be, only need this one Transform")]
    public Transform cameraFixedPose;
    [Tooltip("Time to move into fixed position")]
    public float cameraMoveInTime = 1.0f;

    [Header("Camera: FOV (Optional)")]
    public bool lockFOVDuringCinematic = true;
    public float fixedFOV = 55f;

    // ============= Transition from Fixed Camera → Follow =============
    [Header("Camera: Switch from Fixed to Follow")]
    [Tooltip("Optional: manually set a handoff position (world space). Camera will smoothly move from fixed position to here first, then hand back to CameraFollow. Leave empty to hand back directly.")]
    public Transform followHandoffPose; // optional
    public float handoffLerpTime = 0.8f;
    public bool restoreFOVAfter = true;
    public float followResumeFOV = 60f;

    [Header("Animation Settings")]
    public float animationDuration = 3f;

    [Header("Climb Prerequisites (Cookies)")]
    public int requiredCookies = 3;
    public bool consumeOnClimb = true;
    public FloodSequence flood;

    [Header("Hint UI (When Not Enough)")]
    public CanvasGroup hintGroup;
    public TMP_Text hintText;
    public string notEnoughText = "能量不足（需要 3 个饼干碎屑）";
    public float hintFadeTime = 0.2f;
    public float hintStayTime = 1.2f;

    // ==== SFX: Fixed Camera Start Sound (Play Once, Won't Be Interrupted) ====
    [Header("SFX: Fixed Camera Start Sound (Play Once)")]
    public AudioClip cinematicStartSfx;
    [Range(0f,1f)] public float cinematicStartVolume = 1f;
    [Tooltip("2D=not affected by space; 3D=rendered by world position (will use cameraFixedPose position)")]
    public bool cinematicStartAs2D = true;
    public AudioMixerGroup sfxOutput;      // optional: attach to your SFX Mixer group

    [Header("SFX: Fade In/Natural End Fade Out")]
    public bool sfxUseFade = true;
    public float sfxFadeInTime  = 0.25f;   // fade in at start
    public bool sfxFadeOutAtEnd = true;    // fade out before natural end
    public float sfxEndFadeTime = 0.25f;   // end fade duration

    [Header("SFX: 3D Parameters (Only When 3D)")]
    public float sfxSpatialMinDistance = 1f;
    public float sfxSpatialMaxDistance = 30f;

    private AudioSource _sfx;                  // local audio source
    private Coroutine _fadeCo;                 // volume fade coroutine
    private Coroutine _playOnceCo;             // main play once coroutine
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
        // find player
        playerObject = playerObject ? playerObject : GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerInputController = playerObject.GetComponent<PlayerInputController>();
            playerController = playerObject.GetComponent<PlayerController>();
        }

        // find camera
        cameraFollow = Camera.main ? Camera.main.GetComponent<CameraFollow>() : null;

        // animation substitute
        if (antsAnimationObject != null)
        {
            antsAnimator = antsAnimationObject.GetComponent<Animator>();
            antsAnimationObject.SetActive(false);
        }

        if (hintGroup) hintGroup.alpha = 0f;

        // ==== SFX: initialize audio source ====
        _sfx = gameObject.GetComponent<AudioSource>();
        if (!_sfx) _sfx = gameObject.AddComponent<AudioSource>();
        _sfx.playOnAwake = false;
        _sfx.loop = false;
        _sfx.spatialBlend = cinematicStartAs2D ? 0f : 1f;
        _sfx.minDistance = Mathf.Max(0.01f, sfxSpatialMinDistance);
        _sfx.maxDistance = Mathf.Max(_sfx.minDistance + 0.01f, sfxSpatialMaxDistance);
        if (sfxOutput) _sfx.outputAudioMixerGroup = sfxOutput;
        _sfx.volume = 0f; // start at 0 for fade in
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

    // —— cookie check —— //
    void TryActivate()
    {
        var inv = CookiesInventory.Instance;
        int have = inv ? inv.cookies : 0;

        if (inv == null)
        {
            Debug.LogWarning("[CheckpointUp] CookiesInventory.Instance is null, skipping check (debug mode - treat as passed)");
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

    // —— main flow —— //
    void ActivateCheckpoint()
    {
        if (isAnimating) return;
        if (!cameraFixedPose)
        {
            Debug.LogWarning("[CheckpointUp] cameraFixedPose (fixed camera position) not set, will skip camera fixing.");
        }

        isAnimating = true;

        if (playerInputController) playerInputController.DisableInput();
        if (playerController)      playerController.enabled = false;

        // Broadcast climbing success event immediately when climbing starts
        OnClimbingSuccess?.Invoke();
        Debug.Log("CheckpointUp: Climbing success event broadcasted (at start)");

        // hand over camera follow control (we want to control it ourselves)
        if (cameraFollow) cameraFollow.SetCameraControl(false);

        if (antsAnimationObject) antsAnimationObject.SetActive(false);
        if (playerObject)        playerObject.SetActive(true);

        // ==== SFX: start playing (play entire clip; won't be interrupted by cutscene end) ====
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

        // 1) move from current camera → fixed position (and switch to fixed FOV)
        if (cam && cameraFixedPose)
        {
            float startFov = cam.fieldOfView;
            float endFov   = (lockFOVDuringCinematic ? fixedFOV : startFov);
            yield return LerpCamera(cam,
                                    cam.transform.position, cam.transform.rotation, startFov,
                                    cameraFixedPose.position, cameraFixedPose.rotation, endFov,
                                    cameraMoveInTime);
        }

        // 2) immediately teleport player to "new landing point" to avoid bouncing back
        if (playerObject && playerFinalPosition)
        {
            playerObject.transform.SetPositionAndRotation(
                playerFinalPosition.position, playerFinalPosition.rotation);
        }

        // 3) play substitute animation (camera stays fixed at this point)
        if (antsAnimationObject)
        {
            antsAnimationObject.SetActive(true);
            if (antsAnimator && !string.IsNullOrEmpty(antsAnimationTrigger))
                antsAnimator.SetTrigger(antsAnimationTrigger);
        }
        if (playerObject) playerObject.SetActive(false); // use substitute for performance

        yield return new WaitForSeconds(animationDuration);

        // 4) substitute exits, player appears (already in new position)
        if (antsAnimationObject) antsAnimationObject.SetActive(false);
        if (playerObject)        playerObject.SetActive(true);

        // 5) from fixed position → follow (with handoff position / without handoff position)
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

        // note: don't Stop SFX here anymore (let it finish naturally)
        if (flood) flood.OnClimbSucceeded(resetCrumbs: false);
        poisonShooter.enabled = true;
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

    // —— camera interpolation (position/rotation/FOV interpolated together) ——
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

    // ==== SFX: play entire clip (can fade in, end can fade out), won't be interrupted externally ====
    void PlayCinematicStartOnce()
    {
        if (!_sfx || !cinematicStartSfx) return;

        // when 3D, put audio source at fixed camera position (closer to picture)
        if (!cinematicStartAs2D && cameraFixedPose)
            _sfx.transform.position = cameraFixedPose.position;

        _sfx.spatialBlend = cinematicStartAs2D ? 0f : 1f;
        _sfx.clip = cinematicStartSfx;
        _sfx.loop = false;

        // stop ongoing fade and play
        if (_fadeCo != null) { StopCoroutine(_fadeCo); _fadeCo = null; }
        if (_playOnceCo != null) { StopCoroutine(_playOnceCo); _playOnceCo = null; }

        _playOnceCo = StartCoroutine(CoPlayOnceWithFades());
    }

    IEnumerator CoPlayOnceWithFades()
    {
        _sfx.Stop();
        _sfx.volume = sfxUseFade ? 0f : Mathf.Clamp01(cinematicStartVolume);
        _sfx.Play();

        // fade in at start
        if (sfxUseFade && sfxFadeInTime > 0f)
        {
            yield return FadeVolume(_sfx.volume, Mathf.Clamp01(cinematicStartVolume), sfxFadeInTime);
        }
        else
        {
            _sfx.volume = Mathf.Clamp01(cinematicStartVolume);
        }

        // calculate end fade out start time (within natural length)
        if (sfxFadeOutAtEnd && sfxUseFade && sfxEndFadeTime > 0f)
        {
            float wait = Mathf.Max(0f, cinematicStartSfx.length - sfxEndFadeTime);
            yield return new WaitForSeconds(wait);
            // end fade out
            yield return FadeVolume(_sfx.volume, 0f, sfxEndFadeTime);
            _sfx.Stop();
        }
        else
        {
            // don't do end fade out: just let it finish naturally
            yield return new WaitForSeconds(cinematicStartSfx.length - (_sfx.isPlaying ? _sfx.time : 0f));
            // don't call Stop(), let the tail end naturally
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

    // —— simple hint —— //
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
