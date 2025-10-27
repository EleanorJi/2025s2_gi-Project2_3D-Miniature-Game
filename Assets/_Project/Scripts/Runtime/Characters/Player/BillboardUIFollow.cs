using UnityEngine;

public class BillboardUIFollow : MonoBehaviour
{
    [Header("Follow")]
    public Transform target;                 // 玩家或要跟随的物体
    public bool followTarget = true;         // 是否跟随位置
    public Vector3 worldOffset = new Vector3(0f, 0.6f, 0f); // 手动偏移（世界单位）
    public bool useInitialOffset = true;     // ✔ 自动读取当前摆放位置作为偏移

    [Header("Face Camera")]
    public Camera cam;                       // 留空自动找主相机
    public bool faceCamera = true;
    public bool yawOnly = true;              // 仅绕Y轴对相机

    Vector3 _capturedOffset;
    bool _hasCaptured;

    void Awake()
    {
        if (!cam) cam = Camera.main ?? FindObjectOfType<Camera>();

        // 如果需要自动记住偏移：记录“当前血条位置 - 目标位置”
        if (target && useInitialOffset && !_hasCaptured)
        {
            _capturedOffset = transform.position - target.position;
            _hasCaptured = true;
        }
    }

    void LateUpdate()
    {
        if (followTarget && target)
        {
            Vector3 offset = useInitialOffset && _hasCaptured ? _capturedOffset : worldOffset;
            transform.position = target.position + offset;
        }

        if (faceCamera && cam)
        {
            if (yawOnly)
            {
                var fwd = cam.transform.forward; fwd.y = 0f;
                if (fwd.sqrMagnitude > 1e-6f) transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
            }
            else
            {
                var toCam = cam.transform.position - transform.position;
                if (toCam.sqrMagnitude > 1e-6f) transform.rotation = Quaternion.LookRotation(-toCam.normalized, Vector3.up);
            }
        }
    }
}
