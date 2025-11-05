using UnityEngine;
using System.Collections;
using UnityEngine.AI;
using UnityEngine.SceneManagement; // NEW: scene loading

[DefaultExecutionOrder(-1000)]
public class CameraCinematicSequence : MonoBehaviour
{
    [Header("Dependencies (Optional)")]
    public CameraFollow cameraFollow;
    public PlayerInputController playerInput;

    // ------------------ Stage 1: 1→(stop)→1.1→1.2→1 ------------------
    [Header("Stage 1: Fixed Pose Points + Time")]
    public Transform point1;
    public float holdAtP1 = 2f;

    public Transform point1_1;
    public Transform point1_2;
    public float p1To11Time = 0.8f;
    public float p11To12Time = 0.8f;
    public float p12To1Time = 0.8f;

    public Transform point2;
    public Transform point3;                  // only use rotation
    public float p1To2Time = 3f;
    public float p2To1Time = 3f;
    public float rot1To3Time = 2f;
    public float rot3To1Time = 2f;
    public float waitAfterStage1 = 2f;

    // ------------------ Stage 2: 1→4 Parabolic (only move, lock rotation) ------------------
    [Header("Stage 2: From P1 Takeoff to P4 (only move, no rotation change)")]
    public Transform stage2End;               // 4
    public float stage2Duration = 5f;
    public float arcHeight = 3f;
    public bool lockRotationInStage2 = true;

    [Tooltip("How many seconds before landing to hide the 'fake ant+leaf' in front of camera, only do camera movement")]
    public float hideLeafBeforeLanding = 0.2f;

    // ------------------ Stage 2.5: 4→5→6 (additional shots) ------------------
    [Header("Stage 2.5: Two more segments after landing")]
    public Transform point5;                  // only move
    public float landTo5Time = 1.2f;
    public Transform point6;                  // only rotate in place
    public float rot5To6Time = 0.8f;

    // ------------------ Flying Phase: Fake Leaf/Ant in Front of Camera ------------------
    [Header("Fake 'Ant+Leaf' in Front of Camera (Flying Phase)")]
    public GameObject leafCarryVisualPrefab;
    public Vector3 leafLocalPos = new Vector3(0f, -0.15f, 0.7f);
    public Vector3 leafLocalEuler = new Vector3(0f, 90f, 0f);  // you already tuned this
    public Vector3 leafLocalScale = Vector3.one;
    public bool forceLeafLayerToCamera = true;
    public bool makeLeafPureVisual = true;

    // ------------------ Leaf That "Lands Together" After Landing ------------------
    [Header("Leaf That Lands Together After Landing (Appears Synchronized with Real Ant)")]
    public GameObject landingLeafVisualPrefab;      // can be different from flying one
    public Transform landingLeafSpawnPoint;         // can be empty
    public bool attachLandingLeafToPlayer = true;   // attach to player to land together
    public Vector3 landingLeafLocalOffset = new Vector3(0f, 0.2f, 0f);

    [Tooltip("Use Renderer.bounds to align landing leaf visual center to spawn point, fix FBX root offset")]
    public bool landingLeafUseBoundsCenter = true;
    [Tooltip("Add an extra Euler angle (degrees) to landing leaf")]
    public Vector3 landingLeafEuler = Vector3.zero;

    // ------------------ Sound Effects ------------------
    [Header("SFX: Shared Source (Can Coexist with Independent Sources Below)")]
    public AudioSource sfxSource;   // can be used for one-shot effects (like traffic light/pigeon) if individual source is empty will fall back to this

    [Header("SFX: Traffic Light (One-shot)")]
    public AudioClip trafficLightSfx;
    [Range(0f, 1f)] public float trafficLightVolume = 1f;
    public float trafficLightDelay = 0.5f;
    public bool trafficLightUseFade = true;
    public float trafficLightFadeIn = 0.25f;
    public float trafficLightFadeOut = 0.25f;

    [Header("SFX: Wind (During Stage 2)")]
    public AudioSource windSource;
    public AudioClip windLoop;
    [Range(0f, 1f)] public float windVolume = 0.8f;
    public bool windLooping = true;       // loop during stage 2, fade out when stage 2 ends
    public float windFadeIn = 0.4f;
    public float windFadeOut = 0.4f;

    [Header("SFX: Pigeon (Stage 3)")]
    public AudioSource pigeonSource;      // optional; will use sfxSource if empty
    public AudioClip pigeonSfx;
    [Range(0f, 1f)] public float pigeonVolume = 1f;
    public bool pigeonLoop = true;        // loop during stage 3 wait period
    public float pigeonFadeIn = 0.15f;
    public float pigeonFadeOut = 0.15f;

    // —— Stage 3 Wait: Must Exist —— //
    [Header("Stage 3: Stay After Landing + Pigeon Sound Effect (End)")]
    public float waitAfterLanding = 2f;

    [Header("Landing Performance Behavior (Don't Move Real Player)")]
    public bool keepLeafVisualAtLanding = false;

    // ------------------ Real Player Respawn (Triggered When Reaching "6") ------------------
    [Header("Player Respawn/Landing (Triggered When Reaching 6)")]
    public Transform respawnPoint;
    public Transform playerRoot;
    public bool enablePlayerGravity = true; // pure free-fall: just turn on gravity
    public bool wakeUpPlayerRigidBody = true;

    // ------------------ Camera Lock ------------------
    [Header("Camera: Lock View Angle (Prevent 'Narrowing')")]
    public bool hardLockFOV = true;
    public float lockedFOV = 60f;
    public bool restoreFOVOnFinish = false;

    [Header("General")]
    public bool zeroVelOnStart = true;
    public bool reenableFollowAfter = false;
    public bool reenableInputAfter = false;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public bool showLeafGizmo = true;

    [Header("Debug")]
    public bool debugLogs = false;

    // NEW: Level Switch (After reaching 6 and completing pigeon sound effect, wait N seconds to load next level)
    [Header("Level Switch (Automatically Enter Next Level After Reaching 6)")]
    public bool loadNextAtFinish = true;           // switch
    public string nextLevelName = "Level3";        // scene name (need to add to Build Settings)
    public float holdAtPoint6BeforeLoad = 2f;      // stay duration after reaching 6 and completing sound effects

    public bool IsPlaying { get; private set; }

    // runtime
    GameObject _leafInstance;   // fake leaf for flying
    GameObject _landingLeaf;    // leaf that lands together
    bool _leafHidden;
    Quaternion _cachedStage2Rotation;
    float _windOrigVol = 0f;
    Camera _cam;
    float _origFOV = 60f;
    float _lockedFOVRuntime = 60f;


    public CanvasGroup fadePanel;     
    public float fadeDuration = 1f;  
    public bool useFadeBeforeLoad = true; 




    void Reset() { cameraFollow = GetComponent<CameraFollow>(); }
    void Awake()
    {
        if (!cameraFollow) cameraFollow = GetComponent<CameraFollow>();
        if (!playerInput) playerInput = FindFirstObjectByType<PlayerInputController>(FindObjectsInactive.Exclude);
        _cam = GetComponent<Camera>();
        if (_cam) _origFOV = _cam.fieldOfView;
        if (windSource) windSource.dopplerLevel = 0f;
    }

    void OnEnable() { if (hardLockFOV) StartCoroutine(CoHardLockCameraParams()); }
    void OnDisable() { StopAllCoroutines(); }

    public void Play()
    {
        if (!gameObject.activeInHierarchy || IsPlaying) return;
        if (!point1 || !point2 || !point3 || !stage2End)
        {
            Debug.LogWarning("[CameraCinematicSequence] Missing required waypoints (point1/point2/point3/stage2End)");
            return;
        }
        StartCoroutine(CoPlay());
    }

    IEnumerator CoPlay()
    {
        IsPlaying = true;
        SafeSetFollow(false);
        SafeSetInput(false);

        if (_cam && hardLockFOV)
        {
            _lockedFOVRuntime = (lockedFOV > 0f) ? lockedFOV : _cam.fieldOfView;
            _cam.fieldOfView = _lockedFOVRuntime;
        }

        // Start: snap to 1
        transform.SetPositionAndRotation(point1.position, point1.rotation);

        if (zeroVelOnStart)
        {
            var rb = GetComponent<Rigidbody>(); if (rb) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            var rb2 = GetComponent<Rigidbody2D>(); if (rb2) { rb2.linearVelocity = Vector2.zero; rb2.angularVelocity = 0f; }
        }

        // —— Stage 1 SFX: traffic light —— //
        if (trafficLightSfx)
        {
            var src = sfxSource;
            if (!src) src = gameObject.AddComponent<AudioSource>();
            StartCoroutine(CoPlayNonLoopSfxWithFades(
                src, trafficLightSfx, trafficLightVolume,
                trafficLightDelay,
                trafficLightUseFade ? trafficLightFadeIn : 0f,
                trafficLightUseFade ? trafficLightFadeOut : 0f
            ));
        }

        // Stage 1: hold at 1 for 2s
        if (holdAtP1 > 0f) yield return new WaitForSeconds(holdAtP1);

        // Look around: 1→1.1→1.2→1 (full pose lerp)
        if (point1_1) yield return MovePose(point1, point1_1, p1To11Time);
        if (point1_2) yield return MovePose(point1_1 ? point1_1 : point1, point1_2, p11To12Time);
        yield return MovePose(point1_2 ? point1_2 : point1, point1, p12To1Time);

        // 1 → 2 → 1
        yield return MovePose(point1, point2, p1To2Time);
        yield return MovePose(point2, point1, p2To1Time);

        // At 1: rotate to 3, then rotate back to 1
        yield return RotateAtPosition(point1.position, point1.rotation, point3.rotation, rot1To3Time);
        yield return RotateAtPosition(point1.position, point3.rotation, point1.rotation, rot3To1Time);

        // Wait a bit
        if (waitAfterStage1 > 0f) yield return new WaitForSeconds(waitAfterStage1);

        // —— Stage 2: prep for flight (attach fake leaf, fade wind in, lock rotation) —— //
        _cachedStage2Rotation = transform.rotation;

        _leafHidden = false;

        if (leafCarryVisualPrefab)
        {
            _leafInstance = Instantiate(leafCarryVisualPrefab, transform);
            _leafInstance.transform.localPosition = leafLocalPos;
            _leafInstance.transform.localEulerAngles = leafLocalEuler; // already tuned by you
            _leafInstance.transform.localScale = leafLocalScale;
            if (makeLeafPureVisual) MakePureVisual(_leafInstance, true, true);
            if (_cam && forceLeafLayerToCamera) SetLayerRecursively(_leafInstance, _cam.gameObject.layer);
            if (_cam && _leafInstance.transform.localPosition.z <= _cam.nearClipPlane + 0.02f)
            {
                var p = _leafInstance.transform.localPosition; p.z = _cam.nearClipPlane + 0.05f;
                _leafInstance.transform.localPosition = p;
            }
        }

        // Wind: fade in at the start of Stage 2
        if (windLoop && windSource)
        {
            _windOrigVol = windSource.volume;
            yield return StartCoroutine(CoStartLoopWithFade(windSource, windLoop, windVolume, windFadeIn, windLooping));
        }

        // —— Stage 2: 1→4 along a parabola (rotation locked). Hide fake leaf ~0.2s before landing —— //
        Vector3 startPos = point1.position;
        Vector3 endPos = stage2End.position;
        Quaternion fixedRot = _cachedStage2Rotation;

        float t = 0f;
        float hideAtEt = Mathf.Clamp01(1f - (Mathf.Max(0f, hideLeafBeforeLanding) / Mathf.Max(0.0001f, stage2Duration)));

        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, stage2Duration);
            float et = ease.Evaluate(Mathf.Clamp01(t));

            Vector3 flat = Vector3.Lerp(startPos, endPos, et);
            float yOffset = 4f * arcHeight * et * (1f - et);
            transform.position = new Vector3(flat.x, flat.y + yOffset, flat.z);
            if (lockRotationInStage2) transform.rotation = fixedRot;

            if (!_leafHidden && et >= hideAtEt)
            {
                if (_leafInstance) SetVisualVisible(_leafInstance, false);

                _leafHidden = true;
            }
            yield return null;
        }

        // Wrap-up: clean flying fake leaf + fade wind out
        if (_leafInstance)
        {

            //if (!_leafHidden)
            //    SetVisualVisible(_leafInstance, false);

            //if (leafCarryVisualPrefab && !keepLeafVisualAtLanding)
            //    Destroy(_leafInstance);
            //else
            //{
            //    _leafInstance.transform.SetParent(null, true); _leafInstance.transform.position = stage2End.position;
            //}

            // Ensure leaf remains visible at end, no longer destroy at finish
            SetVisualVisible(_leafInstance, true);

            // No longer destroy leaf, directly unparent
            _leafInstance.transform.SetParent(null, true);
            _leafInstance.transform.position = stage2End.position;
        }

        // Regenerate leaf at end stage
        if (leafCarryVisualPrefab)
        {
            Vector3 spawnWorldPos = new Vector3(71f, 1.29f, -339.618073f);

            GameObject newLeaf = Instantiate(leafCarryVisualPrefab, spawnWorldPos, Quaternion.identity);
            // Reset size to 1,1,1
            newLeaf.transform.localScale = Vector3.one;

            newLeaf.transform.rotation = Quaternion.identity;

            if (makeLeafPureVisual)
            {
                MakePureVisual(newLeaf, true, true);
            }

            if (forceLeafLayerToCamera && _cam)
                SetLayerRecursively(newLeaf, _cam.gameObject.layer);
            _leafInstance = newLeaf;
        }


        if (windLoop && windSource)
        {
            yield return StartCoroutine(CoStopWithFade(windSource, windFadeOut, _windOrigVol));
        }

        // —— Stage 2.5: 4→5 (move only) —— //
        if (point5)
        {
            Quaternion hold = transform.rotation;
            yield return MoveOnlyKeepRotation(transform.position, point5.position, hold, landTo5Time);
        }

        // —— Stage 2.5: 5→6 (rotate in place) —— //
        if (point6)
        {
            yield return RotateAtPosition(point5 ? point5.position : transform.position,
                                          transform.rotation,
                                          point6.rotation,
                                          rot5To6Time);
        }

        // —— Reached "6": real player respawn with free fall + spawn landing leaf —— //


        //SpawnRealPlayerAtRespawn();     // pure free fall (vel=0, only gravity on)

        // SpawnLandingLeafVisual();       // leaf falls together (can follow the player)

        // Stage 3: pigeon loop, wait, then fade out
        if (pigeonSfx)
        {
            var src = pigeonSource ? pigeonSource : sfxSource;
            if (!src) src = gameObject.AddComponent<AudioSource>();
            yield return StartCoroutine(CoStartLoopWithFade(src, pigeonSfx, pigeonVolume, pigeonFadeIn, pigeonLoop));
        }

        if (waitAfterLanding > 0f) yield return new WaitForSeconds(waitAfterLanding);

        if (pigeonSfx)
        {
            var src = pigeonSource ? pigeonSource : sfxSource;
            if (src) yield return StartCoroutine(CoStopWithFade(src, pigeonFadeOut, src.volume));
        }

        // NEW: after pigeon fades out → hold at 6 for N seconds → load next level
        if (loadNextAtFinish && !string.IsNullOrEmpty(nextLevelName))
        {
            //if (holdAtPoint6BeforeLoad > 0f)
            //    yield return new WaitForSeconds(holdAtPoint6BeforeLoad);

            //if (debugLogs) Debug.Log($"[Cinematic] Loading next scene: using scene order transition");
            //SceneOrderManager.Instance.LoadNextScene();
            //yield break; // scene switched, end coroutine

            if (useFadeBeforeLoad)
                yield return StartCoroutine(CoFadeToBlackAndLoadNext());
            else
            {
                if (holdAtPoint6BeforeLoad > 0f) yield return new WaitForSeconds(holdAtPoint6BeforeLoad);
                if (debugLogs) Debug.Log("[Cinematic] Loading next scene: using scene order transition");
                SceneOrderManager.Instance.LoadNextScene();
            }
            yield break;

        }

        if (restoreFOVOnFinish && _cam) _cam.fieldOfView = _origFOV;
        IsPlaying = false;
    }

    private IEnumerator CoFadeToBlackAndLoadNext()
    {
 
        if (fadePanel != null)
        {
            float start = fadePanel.alpha;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                fadePanel.alpha = Mathf.Lerp(start, 1f, t);
                yield return null;
            }
            fadePanel.alpha = 1f;
        }


        SceneOrderManager.Instance.LoadNextScene();
    }


    // ------------------ Lock FOV ------------------
    IEnumerator CoHardLockCameraParams()
    {
        while (true)
        {
            if (IsPlaying && _cam && hardLockFOV)
            {
                float targetFov = (_lockedFOVRuntime > 0f) ? _lockedFOVRuntime : _cam.fieldOfView;
                if (!Mathf.Approximately(_cam.fieldOfView, targetFov)) _cam.fieldOfView = targetFov;
            }
            yield return null;
        }
    }

    // ------------------ Transition helpers ------------------
    IEnumerator MovePose(Transform fromPose, Transform toPose, float time)
    {
        float timer = 0f;
        Vector3 p0 = fromPose.position;
        Quaternion r0 = fromPose.rotation;
        Vector3 p1 = toPose.position;
        Quaternion r1 = toPose.rotation;

        while (timer < time)
        {
            timer += Time.deltaTime;
            float t = ease.Evaluate(Mathf.Clamp01(timer / Mathf.Max(0.0001f, time)));
            transform.position = Vector3.Lerp(p0, p1, t);
            transform.rotation = Quaternion.Slerp(r0, r1, t);
            yield return null;
        }
        transform.SetPositionAndRotation(p1, r1);
    }

    IEnumerator MoveOnlyKeepRotation(Vector3 fromPos, Vector3 toPos, Quaternion holdRot, float time)
    {
        float timer = 0f;
        while (timer < time)
        {
            timer += Time.deltaTime;
            float t = ease.Evaluate(Mathf.Clamp01(timer / Mathf.Max(0.0001f, time)));
            transform.position = Vector3.Lerp(fromPos, toPos, t);
            transform.rotation = holdRot;
            yield return null;
        }
        transform.SetPositionAndRotation(toPos, holdRot);
    }

    IEnumerator RotateAtPosition(Vector3 fixedPos, Quaternion fromRot, Quaternion toRot, float time)
    {
        float timer = 0f;
        while (timer < time)
        {
            timer += Time.deltaTime;
            float t = ease.Evaluate(Mathf.Clamp01(timer / Mathf.Max(0.0001f, time)));
            transform.position = fixedPos;
            transform.rotation = Quaternion.Slerp(fromRot, toRot, t);
            yield return null;
        }
        transform.SetPositionAndRotation(fixedPos, toRot);
    }

    // ------------------ SFX helpers ------------------

    IEnumerator CoPlayNonLoopSfxWithFades(AudioSource src, AudioClip clip, float volume, float delay, float fadeIn, float fadeOut)
    {
        if (!src || !clip) yield break;
        if (delay > 0f) yield return new WaitForSeconds(delay);

        src.clip = clip;
        src.loop = false;
        src.volume = (fadeIn > 0f) ? 0f : volume;
        src.Play();

        if (fadeIn > 0f) yield return StartCoroutine(CoFadeVolume(src, src.volume, volume, fadeIn));

        float hold = Mathf.Max(0f, clip.length - fadeOut);
        if (hold > 0f) yield return new WaitForSeconds(hold);

        if (fadeOut > 0f) yield return StartCoroutine(CoFadeVolume(src, src.volume, 0f, fadeOut));
        src.Stop();
        src.volume = volume; // restore
    }

    IEnumerator CoStartLoopWithFade(AudioSource src, AudioClip clip, float volume, float fadeIn, bool loop)
    {
        if (!src || !clip) yield break;
        src.clip = clip;
        src.loop = loop;
        src.volume = (fadeIn > 0f) ? 0f : volume;
        src.Play();
        if (fadeIn > 0f) yield return StartCoroutine(CoFadeVolume(src, src.volume, volume, fadeIn));
    }

    IEnumerator CoStopWithFade(AudioSource src, float fadeOut, float defaultRestore)
    {
        if (!src) yield break;
        if (fadeOut > 0f) yield return StartCoroutine(CoFadeVolume(src, src.volume, 0f, fadeOut));
        src.Stop();
        src.volume = defaultRestore;
    }

    IEnumerator CoFadeVolume(AudioSource src, float from, float to, float duration)
    {
        if (!src || duration <= 0f) { if (src) src.volume = to; yield break; }
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            src.volume = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        src.volume = to;
    }

    // ------------------ Utilities ------------------
    void SafeSetFollow(bool on) { if (!cameraFollow) return; try { cameraFollow.SetCameraControl(on); } catch { cameraFollow.enabled = on; } }
    void SafeSetInput(bool on) { if (!playerInput) return; try { if (on) playerInput.EnableInput(); else playerInput.DisableInput(); } catch { playerInput.enabled = on; } }

    void MakePureVisual(GameObject root, bool removeColliders, bool setRigidbodiesKinematic)
    {
        if (!root) return;
        var rbs = root.GetComponentsInChildren<Rigidbody>(true);
        foreach (var rb in rbs) { try { if (setRigidbodiesKinematic) { rb.isKinematic = true; rb.useGravity = false; } else Destroy(rb); } catch { } }
        var cols = root.GetComponentsInChildren<Collider>(true);
        foreach (var c in cols) { try { if (removeColliders) Destroy(c); else c.enabled = false; } catch { } }
        var anims = root.GetComponentsInChildren<Animator>(true);
        foreach (var a in anims) { try { a.enabled = false; } catch { } }
        var animsLegacy = root.GetComponentsInChildren<Animation>(true);
        foreach (var a in animsLegacy) { try { a.enabled = false; } catch { } }
        var skins = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var s in skins) { try { s.updateWhenOffscreen = true; s.enabled = true; } catch { } }
        var rends = root.GetComponentsInChildren<Renderer>(true);
        foreach (var r in rends) { try { r.enabled = true; } catch { } }
    }
    void SetLayerRecursively(GameObject go, int layer) { go.layer = layer; foreach (Transform c in go.transform) SetLayerRecursively(c.gameObject, layer); }
    void SetVisualVisible(GameObject root, bool visible) { if (!root) return; foreach (var r in root.GetComponentsInChildren<Renderer>(true)) { if (r) r.enabled = visible; } }

    // ------------------ Real player respawn (triggered at 6) ------------------
    void SpawnRealPlayerAtRespawn()
    {
        if (!playerRoot || !respawnPoint) { if (debugLogs) Debug.LogWarning("[Cinematic] Missing playerRoot/respawnPoint."); return; }
        if (!playerRoot.gameObject.activeSelf) playerRoot.gameObject.SetActive(true);

        var anim = playerRoot.GetComponent<Animator>(); bool prevRM = false; if (anim) { prevRM = anim.applyRootMotion; anim.applyRootMotion = false; }
        var cc = playerRoot.GetComponent<CharacterController>(); bool ccWas = false; if (cc) { ccWas = cc.enabled; cc.enabled = false; }
        var agent = playerRoot.GetComponent<NavMeshAgent>(); bool agentWas = false; if (agent) { agentWas = agent.enabled; agent.enabled = true; }

        var rb = playerRoot.GetComponent<Rigidbody>(); if (rb) { rb.isKinematic = false; rb.useGravity = enablePlayerGravity; }

        bool warped = false;
        if (agent) { warped = agent.Warp(respawnPoint.position); playerRoot.rotation = respawnPoint.rotation; }
        if (!warped)
        {
            playerRoot.SetPositionAndRotation(respawnPoint.position, respawnPoint.rotation);
            if (rb) { rb.position = respawnPoint.position; rb.rotation = respawnPoint.rotation; }
        }
        Physics.SyncTransforms();

        if (rb)
        {
            // Pure free fall: velocity = 0, only gravity on; let physics handle acceleration
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            if (wakeUpPlayerRigidBody) rb.WakeUp();
        }

        if (cc) cc.enabled = ccWas;
        if (agent) agent.enabled = agentWas;
        if (anim) anim.applyRootMotion = prevRM;

        if (debugLogs) Debug.Log("[Cinematic] Player respawned at 6 (free fall).");
    }

    // ------------------ Landing leaf appears at 6 (can attach to player to fall together) ------------------
    void SpawnLandingLeafVisual()
    {
        if (!landingLeafVisualPrefab) return;

        // 1) Initial pose
        Vector3 pos; Quaternion rot;
        if (landingLeafSpawnPoint) { pos = landingLeafSpawnPoint.position; rot = landingLeafSpawnPoint.rotation; }
        else if (playerRoot) { pos = playerRoot.position + landingLeafLocalOffset; rot = playerRoot.rotation; }
        else { pos = transform.position + transform.forward * 0.6f; rot = transform.rotation; }

        _landingLeaf = Instantiate(landingLeafVisualPrefab, pos, rot);
        // If needed, make it pure visual too: MakePureVisual(_landingLeaf, true, true);

        // 2) Center by render bounds (fix FBX root offset)
        if (landingLeafUseBoundsCenter)
        {
            var b = CalcCombinedBounds(_landingLeaf);
            if (b.size.sqrMagnitude > 0f)
            {
                Vector3 worldCenter = b.center;
                Vector3 delta = worldCenter - _landingLeaf.transform.position;
                _landingLeaf.transform.position -= delta;
            }
        }

        // 3) Extra Euler offset
        if (landingLeafEuler != Vector3.zero)
            _landingLeaf.transform.rotation = Quaternion.Euler(landingLeafEuler) * _landingLeaf.transform.rotation;

        // 4) Follow the real ant down together
        if (attachLandingLeafToPlayer && playerRoot)
        {
            _landingLeaf.transform.SetParent(playerRoot, true);
            _landingLeaf.transform.localPosition = landingLeafLocalOffset;
        }
    }

    Bounds CalcCombinedBounds(GameObject root)
    {
        var rends = root.GetComponentsInChildren<Renderer>(true);
        Bounds b = new Bounds();
        bool inited = false;
        foreach (var r in rends)
        {
            if (!r) continue;
            if (!inited) { b = r.bounds; inited = true; }
            else b.Encapsulate(r.bounds);
        }
        return b;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (showLeafGizmo && leafCarryVisualPrefab)
        {
            Gizmos.color = new Color(0f, 1f, 0.7f, 0.35f);
            var m = transform.localToWorldMatrix;
            var wpos = m.MultiplyPoint(leafLocalPos);
            Gizmos.DrawSphere(wpos, 0.05f);
            Gizmos.DrawLine(transform.position, wpos);
        }
    }
#endif
}
