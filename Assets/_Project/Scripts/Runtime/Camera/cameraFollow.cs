using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("目标与偏移")]
    public Transform target;
    public Vector3 offset = new Vector3(0f, 1.5f, -4f);

    [Header("跟随设置")]
    public float smoothSpeed = 5f;
    public float mouseSensitivity = 3f;
    public float minPitch = -35f;
    public float maxPitch = 60f;

    private float yaw = 0f;
    private float pitch = 10f;
    private bool cameraControlEnabled = true;

    // 公开属性供其他脚本访问
    public Transform Target => target;
    public Vector3 Offset => offset;
    public float Yaw => yaw;
    public float Pitch => pitch;

    void Start()
    {
        if (target != null)
        {
            transform.position = target.position + offset;
            transform.LookAt(target);
            yaw = target.eulerAngles.y;
        }
    }

    void LateUpdate()
    {
        if (target == null || !cameraControlEnabled) return;

        // 鼠标输入控制
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // 计算目标位置和旋转
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 desiredPosition = target.position + rotation * offset;

        // 平滑移动
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.LookAt(target);
    }

    public void SetCameraControl(bool enabled)
    {
        cameraControlEnabled = enabled;
    }
}