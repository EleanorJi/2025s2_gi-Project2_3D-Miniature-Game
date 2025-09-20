using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("移动与跳跃参数")]
    // 移动速度
    public float moveSpeed = 5f;
    // 跳跃力度
    public float jumpForce = 7f;

    [Header("拾取参数")]
    public Transform carryPoint;   // 背上挂载点
    private GameObject carriedItem;
    private GameObject nearbyItem;

    [Header("死亡与重生参数")]
    public float maxSafeFallDistance = 5f; // 下落速度阈值
    public Transform respawnPoint;           // 存档点
    private float lastAirY;   // 玩家上一次离开地面时的Y
    private bool wasGrounded; // 用于检测刚刚落地

    // 引用刚体组件
    private Rigidbody rb;
    // 引用主摄像机
    private Camera mainCamera;
    // 是否在地面上
    private bool isGrounded;

    void Start()
    {
        // 获取组件
        rb = GetComponent<Rigidbody>();
        mainCamera = Camera.main; // 获取主摄像机
    }

    void Update()
    {
        // 处理跳跃输入
        HandleJump();
        HandlePickup();
        // 记录离开地面瞬间的高度
        if (!isGrounded && wasGrounded)
        {
            lastAirY = transform.position.y;
        }
        wasGrounded = isGrounded;
    }

    void FixedUpdate()
    {
        // 处理移动
        HandleMovement();
    }

    void HandleMovement()
    {
        // 获取键盘输入
        float horizontalInput = Input.GetAxis("Horizontal"); // A/D
        float verticalInput = Input.GetAxis("Vertical");     // W/S

        // 基于摄像机方向计算移动方向
        Vector3 cameraForward = mainCamera.transform.forward;
        Vector3 cameraRight = mainCamera.transform.right;

        // 忽略摄像机的Y分量
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        // 计算移动方向
        Vector3 movement = (cameraForward * verticalInput) + (cameraRight * horizontalInput);

        if (movement.magnitude > 1f)
        {
            movement.Normalize();
        }

        // 应用移动速度
        movement *= moveSpeed;

        // 保留Y速度（重力 & 跳跃）
        movement.y = rb.linearVelocity.y;

        // 设置刚体速度
        rb.linearVelocity = movement;

        // 让角色面向移动方向
        if (new Vector3(movement.x, 0f, movement.z).magnitude > 0.1f)
        {
            Vector3 lookDirection = new Vector3(movement.x, 0f, movement.z);
            transform.forward = lookDirection.normalized;
        }
    }

    void HandleJump()
    {
        // 检测是否按下空格键并且角色在地面上
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            // 应用向上的力来实现跳跃
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }
    }

    void HandlePickup()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (carriedItem == null && nearbyItem != null)
            {
                carriedItem = nearbyItem;
                carriedItem.transform.SetParent(carryPoint);
                carriedItem.transform.localPosition = Vector3.zero;
                carriedItem.transform.localRotation = Quaternion.identity;

                Rigidbody itemRb = carriedItem.GetComponent<Rigidbody>();
                if (itemRb) itemRb.isKinematic = true;

                Debug.Log("Picked up: " + carriedItem.name);
            }
            else if (carriedItem != null)
            {
                carriedItem.transform.SetParent(null);
                Rigidbody itemRb = carriedItem.GetComponent<Rigidbody>();
                if (itemRb) itemRb.isKinematic = false;

                Debug.Log("Dropped: " + carriedItem.name);
                carriedItem = null;
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;


            // 计算落下高度
            float fallDistance = lastAirY - transform.position.y;
            Debug.Log("lastAirY: " + lastAirY + ", fallDistance: " + fallDistance);
            if (fallDistance > maxSafeFallDistance) // 阈值，单位根据场景调节
            {
                Die();
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Pickup"))
        {
            nearbyItem = other.gameObject;
            Debug.Log("Nearby item: " + nearbyItem.name);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Pickup") && other.gameObject == nearbyItem)
        {
            Debug.Log("Left item: " + other.name);
            nearbyItem = null;
        }
    }

    void Die()
    {
        Debug.Log("Player died!");
        rb.linearVelocity = Vector3.zero; // 重置速度
        transform.position = respawnPoint.position; // 回到存档点
    }
}
