using UnityEngine;
using System.Collections;
using UnityEngine.AI;

[DefaultExecutionOrder(-1000)]
public class CameraCinematicSequence : MonoBehaviour
{
    [Header("依赖（可选）")]
    public CameraFollow cameraFollow;
    public PlayerInputController playerInput;

    // ------------------ 阶段1：1→(停)→1.1→1.2→1 ------------------
    [Header("阶段1：固定姿态点位 + 时间")]
    public Transform point1;
    public float holdAtP1 = 2f;

    public Transform point1_1;
    public Transform point1_2;
    public float p1To11Time  = 0.8f;
    public float p11To12Time = 0.8f;
    public float p12To1Time  = 0.8f;

    public Transform point2;
    public Transform point3;                  // 只用 rotation
    public float p1To2Time   = 3f;
    public float p2To1Time   = 3f;
    public float rot1To3Time = 2f;
    public float rot3To1Time = 2f;
    public float waitAfterStage1 = 2f;

    // ------------------ 阶段2：1→4 抛物线（只移动，锁朝向） ------------------
    [Header("阶段2：从P1起飞到P4（只移动，不变朝向）")]
    public Transform stage2End;               // 4
    public float stage2Duration = 5f;
    public float arcHeight = 3f;
    public bool  lockRotationInStage2 = true;

    [Tooltip("落地前多少秒把镜头前的“假蚂蚁+树叶”隐藏，仅做镜头移动")]
    public float hideLeafBeforeLanding = 0.2f;

    // ------------------ 阶段2.5：4→5→6（补镜头） ------------------
    [Header("阶段2.5：落地后补两段")]
    public Transform point5;                  // 只移动
    public float landTo5Time = 1.2f;
    public Transform point6;                  // 原地只转
    public float rot5To6Time = 0.8f;

    // ------------------ 飞行阶段：镜头前假叶/假蚂蚁 ------------------
    [Header("镜头前 ‘蚂蚁+树叶’（飞行阶段）")]
    public GameObject leafCarryVisualPrefab;
    public Vector3 leafLocalPos   = new Vector3(0f, -0.15f, 0.7f);
    public Vector3 leafLocalEuler = new Vector3(0f, 90f, 0f);  // 你已自行调好
    public Vector3 leafLocalScale = Vector3.one;
    public bool forceLeafLayerToCamera = true;
    public bool makeLeafPureVisual = true;

    // ------------------ 落地后“一起降落”的叶子 ------------------
    [Header("落地后一起降落的叶子（与真蚂蚁同步出现）")]
    public GameObject landingLeafVisualPrefab;      // 可与飞行用不同
    public Transform landingLeafSpawnPoint;         // 可为空
    public bool attachLandingLeafToPlayer = true;   // 绑到玩家一起落
    public Vector3 landingLeafLocalOffset = new Vector3(0f, 0.2f, 0f);

    [Tooltip("用 Renderer.bounds 将落地叶子的视觉中心对齐到生成点，修正FBX根偏移")]
    public bool landingLeafUseBoundsCenter = true;
    [Tooltip("为落地叶子叠加一个额外欧拉角（度）")]
    public Vector3 landingLeafEuler = Vector3.zero;

    // ------------------ 音效 ------------------
    [Header("音效")]
    public AudioSource sfxSource;
    public AudioClip trafficLightSfx;
    public float trafficLightDelay = 0.5f;

    public AudioSource windSource;
    public AudioClip windLoop;
    public float windFadeIn  = 0.4f;
    public float windFadeOut = 0.4f;
    [Range(0f,1f)] public float windVolume = 0.8f;

    [Header("阶段3：落地后停留 + 鸽子音效（结束）")]
    public float waitAfterLanding = 2f;
    public AudioClip pigeonSfx;
    [Range(0f,1f)] public float pigeonVolume = 1f;

    [Header("落地演出行为（不移动真实玩家）")]
    public bool keepLeafVisualAtLanding = false;

    // ------------------ 真玩家重生（到达“6”的瞬间） ------------------
    [Header("玩家重生/降落（到达 6 时触发）")]
    public Transform respawnPoint;
    public Transform playerRoot;
    public bool enablePlayerGravity   = true; // 纯自由落体：只开重力
    public bool wakeUpPlayerRigidBody = true;

    // ------------------ 相机锁定 ------------------
    [Header("相机：锁定视角（防止“收窄”）")]
    public bool hardLockFOV = true;
    public float lockedFOV  = 60f;
    public bool restoreFOVOnFinish = false;

    [Header("通用")]
    public bool zeroVelOnStart = true;
    public bool reenableFollowAfter = false;
    public bool reenableInputAfter  = false;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0,0,1,1);
    public bool showLeafGizmo = true;

    [Header("Debug")]
    public bool debugLogs = false;

    public bool IsPlaying { get; private set; }

    // 运行时
    GameObject _leafInstance;   // 飞行用假叶
    GameObject _landingLeaf;    // 落地一起降落的叶
    bool _leafHidden;
    Quaternion _cachedStage2Rotation;
    float _windOrigVol = 0f;
    Camera _cam;
    float  _origFOV = 60f;
    float  _lockedFOVRuntime = 60f;

    void Reset() { cameraFollow = GetComponent<CameraFollow>(); }
    void Awake()
    {
        if (!cameraFollow) cameraFollow = GetComponent<CameraFollow>();
        if (!playerInput)  playerInput  = FindFirstObjectByType<PlayerInputController>(FindObjectsInactive.Exclude);
        _cam = GetComponent<Camera>();
        if (_cam) _origFOV = _cam.fieldOfView;
        if (windSource) windSource.dopplerLevel = 0f;
    }

    void OnEnable()  { if (hardLockFOV) StartCoroutine(CoHardLockCameraParams()); }
    void OnDisable() { StopAllCoroutines(); }

    public void Play()
    {
        if (!gameObject.activeInHierarchy || IsPlaying) return;
        if (!point1 || !point2 || !point3 || !stage2End)
        {
            Debug.LogWarning("[CameraCinematicSequence] 缺少必要路径点（point1/point2/point3/stage2End）");
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

        // 起始：对齐到 1
        transform.SetPositionAndRotation(point1.position, point1.rotation);

        if (zeroVelOnStart)
        {
            var rb  = GetComponent<Rigidbody>();   if (rb)  { rb.linearVelocity  = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            var rb2 = GetComponent<Rigidbody2D>(); if (rb2) { rb2.linearVelocity = Vector2.zero; rb2.angularVelocity = 0f; }
        }

        // 阶段1：红绿灯音效（+0.5s）
        if (trafficLightSfx && sfxSource)
        {
            sfxSource.Stop(); sfxSource.clip = trafficLightSfx; sfxSource.loop = false;
            sfxSource.PlayDelayed(Mathf.Max(0f, trafficLightDelay));
        }

        // 阶段1：在1停 2s
        if (holdAtP1 > 0f) yield return new WaitForSeconds(holdAtP1);

        // 环顾：1→1.1→1.2→1（全姿态插值）
        if (point1_1) yield return MovePose(point1,  point1_1, p1To11Time);
        if (point1_2) yield return MovePose(point1_1 ? point1_1 : point1, point1_2, p11To12Time);
        yield return MovePose(point1_2 ? point1_2 : point1, point1, p12To1Time);

        // 1→2→1
        yield return MovePose(point1, point2, p1To2Time);
        yield return MovePose(point2, point1, p2To1Time);

        // 1 原地转 3，再转回 1
        yield return RotateAtPosition(point1.position, point1.rotation, point3.rotation, rot1To3Time);
        yield return RotateAtPosition(point1.position, point3.rotation, point1.rotation, rot3To1Time);

        // 等待
        if (waitAfterStage1 > 0f) yield return new WaitForSeconds(waitAfterStage1);

        // —— 阶段2：准备飞行（挂假叶、风声淡入、锁朝向）——
        _cachedStage2Rotation = transform.rotation;
        _leafHidden = false;

        if (leafCarryVisualPrefab)
        {
            _leafInstance = Instantiate(leafCarryVisualPrefab, transform);
            _leafInstance.transform.localPosition    = leafLocalPos;
            _leafInstance.transform.localEulerAngles = leafLocalEuler; // 你已自行调好
            _leafInstance.transform.localScale       = leafLocalScale;
            if (makeLeafPureVisual) MakePureVisual(_leafInstance, true, true);
            if (_cam && forceLeafLayerToCamera) SetLayerRecursively(_leafInstance, _cam.gameObject.layer);
            if (_cam && _leafInstance.transform.localPosition.z <= _cam.nearClipPlane + 0.02f)
            {
                var p = _leafInstance.transform.localPosition; p.z = _cam.nearClipPlane + 0.05f;
                _leafInstance.transform.localPosition = p;
            }
        }

        if (windLoop && windSource)
        {
            _windOrigVol = windSource.volume;
            windSource.clip = windLoop; windSource.loop = true; windSource.volume = 0f; windSource.Play();
            yield return FadeAudio(windSource, 0f, windVolume, windFadeIn);
        }

        // —— 阶段2：1→4 抛物线（锁朝向），落地前0.2s隐藏假叶 —— //
        Vector3 startPos = point1.position;
        Vector3 endPos   = stage2End.position;
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

        // 收尾：清理飞行用假叶 + 风声淡出
        if (_leafInstance)
        {
            if (!_leafHidden) SetVisualVisible(_leafInstance, false);
            if (leafCarryVisualPrefab && !keepLeafVisualAtLanding) Destroy(_leafInstance);
            else { _leafInstance.transform.SetParent(null, true); _leafInstance.transform.position = stage2End.position; }
        }
        if (windLoop && windSource)
        {
            yield return FadeAudio(windSource, windSource.volume, 0f, windFadeOut);
            windSource.Stop(); windSource.volume = _windOrigVol;
        }

        // —— 阶段2.5：4→5（只移动） —— //
        if (point5)
        {
            Quaternion hold = transform.rotation;
            yield return MoveOnlyKeepRotation(transform.position, point5.position, hold, landTo5Time);
        }

        // —— 阶段2.5：5→6（原地只转） —— //
        if (point6)
        {
            yield return RotateAtPosition(point5 ? point5.position : transform.position,
                                          transform.rotation,
                                          point6.rotation,
                                          rot5To6Time);
        }

        // —— 到达“6”：真玩家自由落体重生 + 落地叶子出现 —— //
        SpawnRealPlayerAtRespawn();     // 纯自由落体（速度=0，只开重力）
        SpawnLandingLeafVisual();       // 叶子一起落（可绑玩家）

        // 阶段3：停留 + 鸽子叫
        if (pigeonSfx && sfxSource)
        {
            sfxSource.Stop(); sfxSource.volume = pigeonVolume; sfxSource.PlayOneShot(pigeonSfx);
        }
        if (waitAfterLanding > 0f) yield return new WaitForSeconds(waitAfterLanding);

        if (restoreFOVOnFinish && _cam) _cam.fieldOfView = _origFOV;
        IsPlaying = false;
    }

    // ------------------ 锁FOV ------------------
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

    // ------------------ 过渡工具 ------------------
    IEnumerator MovePose(Transform fromPose, Transform toPose, float time)
    {
        float timer = 0f;
        Vector3    p0 = fromPose.position;
        Quaternion r0 = fromPose.rotation;
        Vector3    p1 = toPose.position;
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

    // ------------------ 实用工具 ------------------
    void SafeSetFollow(bool on){ if (!cameraFollow) return; try { cameraFollow.SetCameraControl(on); } catch { cameraFollow.enabled = on; } }
    void SafeSetInput (bool on){ if (!playerInput)  return; try { if (on) playerInput.EnableInput(); else playerInput.DisableInput(); } catch { playerInput.enabled = on; } }

    IEnumerator FadeAudio(AudioSource src, float from, float to, float duration)
    {
        if (!src || duration <= 0f) { if (src) src.volume = to; yield break; }
        float t = 0f;
        while (t < duration) { t += Time.deltaTime; src.volume = Mathf.Lerp(from, to, t / duration); yield return null; }
        src.volume = to;
    }

    void MakePureVisual(GameObject root, bool removeColliders, bool setRigidbodiesKinematic)
    {
        if (!root) return;
        var rbs = root.GetComponentsInChildren<Rigidbody>(true);
        foreach (var rb in rbs){ try { if (setRigidbodiesKinematic){ rb.isKinematic = true; rb.useGravity = false; } else Destroy(rb);} catch {} }
        var cols = root.GetComponentsInChildren<Collider>(true);
        foreach (var c in cols){ try { if (removeColliders) Destroy(c); else c.enabled = false; } catch {} }
        var anims = root.GetComponentsInChildren<Animator>(true);
        foreach (var a in anims){ try { a.enabled = false; } catch {} }
        var animsLegacy = root.GetComponentsInChildren<Animation>(true);
        foreach (var a in animsLegacy){ try { a.enabled = false; } catch {} }
        var skins = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var s in skins){ try { s.updateWhenOffscreen = true; s.enabled = true; } catch {} }
        var rends = root.GetComponentsInChildren<Renderer>(true);
        foreach (var r in rends){ try { r.enabled = true; } catch {} }
    }
    void SetLayerRecursively(GameObject go, int layer){ go.layer = layer; foreach (Transform c in go.transform) SetLayerRecursively(c.gameObject, layer); }
    void SetVisualVisible(GameObject root, bool visible){ if (!root) return; foreach (var r in root.GetComponentsInChildren<Renderer>(true)){ if (r) r.enabled = visible; } }

    // ------------------ 真玩家重生（在到达6时触发） ------------------
    void SpawnRealPlayerAtRespawn()
    {
        if (!playerRoot || !respawnPoint) { if (debugLogs) Debug.LogWarning("[Cinematic] Missing playerRoot/respawnPoint."); return; }
        if (!playerRoot.gameObject.activeSelf) playerRoot.gameObject.SetActive(true);

        var anim = playerRoot.GetComponent<Animator>(); bool prevRM = false; if (anim){ prevRM = anim.applyRootMotion; anim.applyRootMotion = false; }
        var cc   = playerRoot.GetComponent<CharacterController>(); bool ccWas = false; if (cc){ ccWas = cc.enabled; cc.enabled = false; }
        var agent= playerRoot.GetComponent<NavMeshAgent>(); bool agentWas = false; if (agent){ agentWas = agent.enabled; agent.enabled = true; }

        var rb = playerRoot.GetComponent<Rigidbody>(); if (rb){ rb.isKinematic = false; rb.useGravity = enablePlayerGravity; }

        bool warped = false;
        if (agent){ warped = agent.Warp(respawnPoint.position); playerRoot.rotation = respawnPoint.rotation; }
        if (!warped)
        {
            playerRoot.SetPositionAndRotation(respawnPoint.position, respawnPoint.rotation);
            if (rb){ rb.position = respawnPoint.position; rb.rotation = respawnPoint.rotation; }
        }
        Physics.SyncTransforms();

        if (rb)
        {
            // 纯自由落体：速度=0，只开重力，加速度交给物理
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            if (wakeUpPlayerRigidBody) rb.WakeUp();
        }

        if (cc)    cc.enabled = ccWas;
        if (agent) agent.enabled = agentWas;
        if (anim)  anim.applyRootMotion = prevRM;

        if (debugLogs) Debug.Log("[Cinematic] Player respawned at 6 (free fall).");
    }

    // ------------------ 落地叶子在6出现（可绑玩家一起落） ------------------
    void SpawnLandingLeafVisual()
    {
        if (!landingLeafVisualPrefab) return;

        // 1) 初始位姿
        Vector3 pos; Quaternion rot;
        if (landingLeafSpawnPoint) { pos = landingLeafSpawnPoint.position; rot = landingLeafSpawnPoint.rotation; }
        else if (playerRoot)      { pos = playerRoot.position + landingLeafLocalOffset; rot = playerRoot.rotation; }
        else                      { pos = transform.position + transform.forward * 0.6f; rot = transform.rotation; }

        _landingLeaf = Instantiate(landingLeafVisualPrefab, pos, rot);
        //if (makeLeafPureVisual) MakePureVisual(_landingLeaf, true, true);

        // 2) 用渲染中心居中（修正FBX根偏移）
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

        // 3) 叠加欧拉角
        if (landingLeafEuler != Vector3.zero)
            _landingLeaf.transform.rotation = Quaternion.Euler(landingLeafEuler) * _landingLeaf.transform.rotation;

        // 4) 跟随真蚂蚁一起落
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
            Gizmos.color = new Color(0f,1f,0.7f,0.35f);
            var m = transform.localToWorldMatrix;
            var wpos = m.MultiplyPoint(leafLocalPos);
            Gizmos.DrawSphere(wpos, 0.05f);
            Gizmos.DrawLine(transform.position, wpos);
        }
    }
#endif
}
