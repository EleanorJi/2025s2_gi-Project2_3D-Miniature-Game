using UnityEngine;
using System.Collections;

public class EndLevel2Sequence : MonoBehaviour
{
    [Header("Trigger")]
    public string playerTag = "Player";

    [Header("Player & Camera")]
    public Transform player;                     // 拖 Player
    public PlayerInputController inputCtrl;      // 拖 PlayerInputController
    public CameraFollow camFollow;               // 拖 CameraFollow（主相机上的）
    public Camera cam;                           // 一般就是 Camera.main

    [Header("Shot A: Traffic Light")]
    public Transform camTrafficPos;              // 相机机位
    public Transform camTrafficLookAt;           // 看向点
    public float camTrafficMove = 1.0f;          // 推近时间
    public float camTrafficHold = 1.5f;          // 停留时间
    public Animator trafficLightAnimator;        // 可空
    public string goGreenTrigger = "GoGreen";    // 可空
    public AudioSource melTrafficAudio;          // 可空（墨尔本红绿灯音效）
    public float fovTraffic = 50f;

    [Header("Shot B: Leaf Close-up")]
    public Transform camLeafPos;
    public Transform camLeafLookAt;
    public float camLeafMove = 1.0f;
    public float camLeafHold = 1.2f;
    public float fovLeaf = 45f;

    [Header("Glide (Script Path)")]
    public Transform stuntRig;                   // 替身父节点(初始隐藏)
    public Transform stuntModelRoot;             // 替身的模型根，用来“锁朝向”
    public Animator stuntAnimator;               // 可空
    public string stuntTrigger = "Glide";
    public Transform takeoffPoint;               // 可空；空则用玩家当前位置
    public Transform[] midAirPoints;             // 1~3 个航点
    public Transform landingPoint;               // 必填
    public float pathDuration = 4.0f;            // 总时长（不含停留）
    public AnimationCurve pathEase = null;       // 不填则用 EaseInOut
    public bool orientAlongPath = false;         // 关掉=不沿路径旋转（解决“纸片”）
    public bool lockModelRotation = true;        // 锁定替身模型朝向（强力解决“纸片”）
    public Vector3 lockedEuler = new Vector3(0, 90, 0); // 锁定角度（按需要改）

    [Tooltip("每个航点的停留时长（可选）：长度应和 midAirPoints 一致；不想停就留空或填 0")]
    public float[] midAirHolds;

    [Header("Glide: Cinematic Camera")]
    public bool cinematicFollowDuringGlide = true;
    public Vector3 glideCamLocalOffset = new Vector3(-0.8f, 0.5f, -1.6f); // 相对替身
    public float glideCamLag = 0.2f;
    public float fovGlide = 55f;

    [Header("Landing / Hand off")]
    public bool snapPlayerToLanding = true;      // 把玩家放到落点
    public bool reenableControlAtEnd = true;     // 恢复输入/相机
    public float landingStickKinematicTime = 0.15f; // 落地瞬间短暂 isKinematic
    public LayerMask groundMask;                 // 地面层
    public float groundSnapRay = 3f;             // 向下射线长度
    public float groundSnapOffset = 0.02f;       // 贴地偏移
    public float fovEnd = 60f;

    bool played;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (pathEase == null) pathEase = AnimationCurve.EaseInOut(0,0,1,1);
    }

    void OnTriggerEnter(Collider other)
    {
        if (played) return;
        if (!other.CompareTag(playerTag)) return;
        played = true;
        StartCoroutine(Sequence());
    }

    IEnumerator Sequence()
    {
        // 1) 接管：禁输入、关相机跟随
        if (inputCtrl) inputCtrl.DisableInput();
        if (camFollow) camFollow.SetCameraControl(false);

        // 2) Shot A: 红绿灯
        yield return StartCoroutine(MoveCam(camTrafficPos, camTrafficLookAt, camTrafficMove, fovTraffic));
        if (trafficLightAnimator && !string.IsNullOrEmpty(goGreenTrigger))
            trafficLightAnimator.SetTrigger(goGreenTrigger);
        if (melTrafficAudio) melTrafficAudio.Play();
        yield return new WaitForSeconds(camTrafficHold);

        // 3) Shot B: 叶子特写
        yield return StartCoroutine(MoveCam(camLeafPos, camLeafLookAt, camLeafMove, fovLeaf));
        yield return new WaitForSeconds(camLeafHold);

        // 4) Glide：隐藏玩家→启用替身→飞
        Vector3 startPos = takeoffPoint ? takeoffPoint.position : player.position;
        Quaternion startRot = takeoffPoint ? takeoffPoint.rotation : player.rotation;

        if (player) player.gameObject.SetActive(false);
        if (stuntRig) { stuntRig.gameObject.SetActive(true); stuntRig.position = startPos; stuntRig.rotation = startRot; }

        if (stuntAnimator && !string.IsNullOrEmpty(stuntTrigger))
            stuntAnimator.SetTrigger(stuntTrigger);

        // Glide 摄影机改为跟拍
        float oldFov = cam ? cam.fieldOfView : 60f;
        if (cam) cam.fieldOfView = fovGlide;

        // 锁定替身模型朝向（彻底消除“纸片翻面”的违和感）
        Quaternion lockedRot = Quaternion.Euler(lockedEuler);

        // 组路径：start → midAirPoints → landing
        int segs = (midAirPoints != null ? midAirPoints.Length : 0) + 1;
        Vector3[] waypoints = new Vector3[segs + 1];
        waypoints[0] = startPos;
        for (int i = 0; i < segs - 1; i++) waypoints[i+1] = midAirPoints[i].position;
        waypoints[segs] = landingPoint.position;

        float[] holds = new float[segs]; // 每个中间点停留（最后一个落点不需要）
        for (int i = 0; i < holds.Length; i++)
            holds[i] = (midAirHolds != null && i < midAirHolds.Length) ? Mathf.Max(0, midAirHolds[i]) : 0f;

        // 每段均分时间
        float segTime = Mathf.Max(0.01f, pathDuration / segs);

        for (int i = 0; i < segs; i++)
        {
            Vector3 a = waypoints[i];
            Vector3 b = waypoints[i+1];
            yield return StartCoroutine(MoveStuntSegment(a, b, segTime, lockedRot));

            // 到达中间点后停留
            if (i < holds.Length && holds[i] > 0f)
                yield return new WaitForSeconds(holds[i]);
        }

        // 5) 落地交接：贴地、防穿透、再交还
        if (cam) cam.fieldOfView = fovEnd;

        // 贴地（防止“落不住”）
        Vector3 land = landingPoint.position;
        if (Physics.Raycast(land + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, groundSnapRay, groundMask))
            land = hit.point + Vector3.up * groundSnapOffset;

        // 把替身放到贴地位（保持一会 isKinematic）；再把玩家交过去
        if (stuntRig)
        {
            stuntRig.position = land;
            // 临时抗穿透：如果替身带刚体（通常不会），可以做短暂 isKinematic
            var rb = stuntRig.GetComponentInChildren<Rigidbody>();
            if (rb)
            {
                bool keep = rb.isKinematic;
                rb.isKinematic = true;
                yield return new WaitForSeconds(landingStickKinematicTime);
                rb.isKinematic = keep;
            }
        }

        if (snapPlayerToLanding && player)
        {
            player.position = land;
            var prb = player.GetComponent<Rigidbody>();
            if (prb)
            {
                // 清速度，短暂 isKinematic，避免立刻下坠/弹飞
                bool keep = prb.isKinematic;
                prb.isKinematic = true;
                prb.linearVelocity = Vector3.zero;
                prb.angularVelocity = Vector3.zero;
                yield return new WaitForSeconds(landingStickKinematicTime);
                prb.isKinematic = keep;
            }
        }

        // 收尾：隐藏替身 → 显示玩家
        if (stuntRig) stuntRig.gameObject.SetActive(false);
        if (player)   player.gameObject.SetActive(true);

        // 6) 归还相机与输入
        if (reenableControlAtEnd)
        {
            if (camFollow) camFollow.SetCameraControl(true);
            if (inputCtrl) inputCtrl.EnableInput();
        }
    }

    // —— 相机移动到某机位并看向 —— //
    IEnumerator MoveCam(Transform toPos, Transform lookAt, float dur, float fov)
    {
        if (!cam || !toPos || !lookAt) yield break;

        Vector3   p0 = cam.transform.position;
        Quaternion r0 = cam.transform.rotation;
        float f0 = cam.fieldOfView;

        Vector3   p1 = toPos.position;
        Quaternion r1 = Quaternion.LookRotation((lookAt.position - toPos.position).normalized, Vector3.up);

        for (float t=0; t<dur; t+=Time.deltaTime)
        {
            float k = t / dur;
            cam.transform.position = Vector3.Lerp(p0, p1, Mathf.SmoothStep(0,1,k));
            cam.transform.rotation = Quaternion.Slerp(r0, r1, Mathf.SmoothStep(0,1,k));
            cam.fieldOfView = Mathf.Lerp(f0, fov, Mathf.SmoothStep(0,1,k));
            yield return null;
        }
        cam.transform.position = p1;
        cam.transform.rotation = r1;
        cam.fieldOfView = fov;
    }

    // —— 替身沿一段路径飞行（带可选镜头跟拍 & 朝向锁定）—— //
    IEnumerator MoveStuntSegment(Vector3 a, Vector3 b, float dur, Quaternion lockedRot)
    {
        if (!stuntRig) yield break;

        // Glide 摄影机跟拍：目标机位在替身本地偏移的位置
        Vector3 camVel = Vector3.zero;
        Vector3 camTarget = Vector3.zero;

        for (float t=0; t<dur; t+=Time.deltaTime)
        {
            float k = pathEase.Evaluate(Mathf.Clamp01(t / dur));
            stuntRig.position = Vector3.Lerp(a, b, k);

            if (orientAlongPath)
            {
                Vector3 dir = (b - a).normalized;
                if (dir.sqrMagnitude > 1e-6f)
                    stuntRig.rotation = Quaternion.LookRotation(dir, Vector3.up);
            }
            else if (lockModelRotation && stuntModelRoot)
            {
                // 保持替身模型朝向固定（不被路径影响）
                stuntModelRoot.rotation = lockedRot;
            }

            // 跟拍
            if (cinematicFollowDuringGlide && cam)
            {
                Vector3 desired = stuntRig.TransformPoint(glideCamLocalOffset);
                cam.transform.position = Vector3.Lerp(cam.transform.position, desired, 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.0001f, glideCamLag)));
                cam.transform.rotation = Quaternion.Slerp(
                    cam.transform.rotation,
                    Quaternion.LookRotation((stuntRig.position - cam.transform.position).normalized, Vector3.up),
                    1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.0001f, glideCamLag))
                );
            }

            yield return null;
        }

        stuntRig.position = b;
        if (!orientAlongPath && lockModelRotation && stuntModelRoot)
            stuntModelRoot.rotation = lockedRot;
    }
}
