using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("目标与偏移")]
    public Transform target;                // 摄像机跟随的目标
    public Vector3 offset = new Vector3(0f, 2f, -5f); // 相对于目标的偏移量

    [Header("跟随与旋转设置")]
    public float smoothSpeed = 5f;          // 平滑移动速度
    public float mouseSensitivity = 3f;     // 鼠标灵敏度
    public float minPitch = -35f;           // 垂直旋转下限
    public float maxPitch = 60f;            // 垂直旋转上限

    private float yaw = 0f;   // 水平旋转角度
    private float pitch = 10f; // 垂直旋转角度（初始略微俯视）

    void Start()
    {
        if (target != null)
        {
            // 初始位置 = 主角位置 + 偏移量（相对于主角方向）
            transform.position = target.position + target.right * offset.x + target.up * offset.y + target.forward * offset.z;
            transform.LookAt(target);

            // 初始角度与主角方向一致
            yaw = target.eulerAngles.y;
        }

        // 隐藏鼠标并锁定到屏幕中间（可选）
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        // 鼠标输入
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch); // 限制俯仰角

        // 根据角度计算旋转
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);

        // 目标位置 + 偏移量（旋转后）
        Vector3 desiredPosition = target.position + rotation * offset;

        // 平滑移动
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // 始终看向目标
        transform.LookAt(target);
    }
}
