using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Goals and Deviations")]
    public Transform target;
    public Vector3 offset = new Vector3(0f, 1.5f, -4f);

    [Header("Follow settings")]
    public float smoothSpeed = 5f;
    public float mouseSensitivity = 3f;
    public float minPitch = -35f;
    public float maxPitch = 60f;

    private float yaw = 0f;
    private float pitch = 10f;
    private bool cameraControlEnabled = true;

    // Public attribute for access by other scripts
    public Transform Target => target;
    public Vector3 Offset => offset;
    public float Yaw => yaw;
    public float Pitch => pitch;

    // Lock the vertical viewing angle with
    bool _lockPitch = false;
    float _lockedPitchValue = 0f;

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

        // Mouse input control
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        
        if (_lockPitch)
        {
            pitch = _lockedPitchValue;
        }
        else
        {
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        // Calculate the target position and rotation
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 desiredPosition = target.position + rotation * offset;

        // Smooth movement
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.LookAt(target);
    }

    public void SetPitchLock(bool locked, float pitchValue = 0f)
    {
        _lockPitch = locked;
        if (_lockPitch)
        {
            _lockedPitchValue = pitchValue;
        }
    }

    public void SetCameraControl(bool enabled)
    {
        cameraControlEnabled = enabled;
    }
}