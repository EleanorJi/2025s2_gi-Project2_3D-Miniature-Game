using UnityEngine;

public class BillboardUIFollow : MonoBehaviour
{
    [Header("Follow")]
    public Transform target;                 // follow target
    public bool followTarget = true;        
    public Vector3 worldOffset = new Vector3(0f, 0.6f, 0f); 
    public bool useInitialOffset = true;     // read initial offset from target on Awake

    [Header("Face Camera")]
    public Camera cam;                       // camera to face
    public bool faceCamera = true;
    public bool yawOnly = true;              // only rotate around Y axis

    Vector3 _capturedOffset;
    bool _hasCaptured;

    void Awake()
    {
        if (!cam) cam = Camera.main ?? FindObjectOfType<Camera>();


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
