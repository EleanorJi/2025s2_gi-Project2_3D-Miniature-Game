using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // 移动速度
    public float moveSpeed = 5f;
    // 跳跃力度
    public float jumpForce = 7f;
    
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
        
        // 忽略摄像机的Y轴旋转，使移动保持水平
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        // 计算最终的移动方向
        Vector3 movement = (cameraForward * verticalInput) + (cameraRight * horizontalInput);
        
        // 标准化向量以确保斜向移动不会更快
        if (movement.magnitude > 1f)
        {
            movement.Normalize();
        }

        // 应用移动速度
        movement *= moveSpeed;

        // 保持Y轴速度不变（保留重力和跳跃的影响）
        movement.y = rb.linearVelocity.y;

        // 应用速度到刚体
        rb.linearVelocity = movement;

        // 可选：让角色面向移动方向（如果喜欢可以保留）
        if (movement.magnitude > 0.1f)
        {
            Vector3 lookDirection = new Vector3(movement.x, 0f, movement.z);
            if (lookDirection != Vector3.zero)
            {
                transform.forward = lookDirection.normalized;
            }
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

    // 检测是否接触地面
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }
}