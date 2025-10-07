using UnityEngine;
using Antventure.UI;

public class PlayerController : MonoBehaviour
{
    [Header("移动与跳跃参数")]
    public float moveSpeed = 5f;
    public float jumpForce = 7f;
    private float startThreshold = 0.1f;
    private float stopThreshold = 0.3f;
    
    // 动画控制
    private Animator antAnimator;
    private bool isMoving = false;

    [Header("地面检测参数")]
    public int groundContactCount = 0;

    [Header("拾取参数")]
    public Transform carryPoint;
    public Transform dropPoint;
    private GameObject carriedItem;
    private GameObject nearbyItem;

    [Header("死亡与重生参数")]
    public float maxSafeFallDistance = 5f;
    public Transform respawnPoint;
    private float lastAirY;
    private bool wasGrounded;

    // 组件引用
    private Rigidbody rb;
    private Camera mainCamera;
    public bool IsCarryingItem => carriedItem != null;

    // 输入缓存（在Update中读取，在FixedUpdate中使用）
    private float horizontalInput;
    private float verticalInput;
    private bool jumpInput;
    private bool pickupInput;

    private System.Collections.Generic.List<GameObject> groundContacts = new System.Collections.Generic.List<GameObject>();
    private PlayerInputController inputController;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        mainCamera = Camera.main;
        antAnimator = GetComponentInChildren<Animator>();
        
        if (antAnimator == null)
        {
            Debug.LogError("Animator not found on ant model!");
        }

        // 确保存档点管理器存在
        if (CheckpointManager.Instance == null)
        {
            GameObject checkpointManager = new GameObject("CheckpointManager");
            checkpointManager.AddComponent<CheckpointManager>();
        }

        inputController = GetComponent<PlayerInputController>();
        if (inputController == null)
        {
            Debug.LogError("PlayerInputController not found! Adding one...");
            inputController = gameObject.AddComponent<PlayerInputController>();
        }
        
        // 隐藏光标
        if (CursorManager.Instance != null)
        {
            CursorManager.Instance.HideCursor();
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    void Update()
    {
        // 确保光标隐藏
        if (Cursor.visible)
        {
            Debug.LogWarning("[PLAYER] Cursor became visible during gameplay, hiding it again");
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        if (inputController != null && !inputController.IsInputEnabled())
        {
            // Debug.Log("输入被禁用，跳过玩家输入处理");
            // 控制动画
            StopMovement();
            return;
        }
        
        // 读取输入（在Update中）
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");
        jumpInput = Input.GetKeyDown(KeyCode.Space);
        pickupInput = Input.GetKeyDown(KeyCode.C);

        // 更新移动状态（用于动画）
        UpdateMovementState();
        
        // 更新地面状态
        UpdateGroundState();

        // 立即处理跳跃
        HandleJump();
        HandlePickup();

        // 记录离开地面瞬间的高度
        if (!(groundContactCount > 0) && wasGrounded)
        {
            lastAirY = transform.position.y;
        }
        wasGrounded = groundContactCount > 0;

        // 调试：按R键死亡
        if (Input.GetKeyDown(KeyCode.R))
        {
            Die();
        }
    }

    void FixedUpdate()
    {
        if (inputController != null && !inputController.IsInputEnabled())
        {
            StopMovement();
            return;
        }
        
        // 处理移动
        HandleMovement();
    }

    void UpdateMovementState()
    {
        // 更新移动状态用于动画
        if (isMoving)
        {
            isMoving = Mathf.Abs(horizontalInput) > stopThreshold || Mathf.Abs(verticalInput) > stopThreshold;
        }
        else
        {
            isMoving = Mathf.Abs(horizontalInput) > startThreshold || Mathf.Abs(verticalInput) > startThreshold;
        }

        // 控制动画
        if (antAnimator != null)
        {
            antAnimator.SetBool("IsMoving", isMoving);
        }
    }

    void UpdateGroundState()
    {
        bool newGroundedState = groundContactCount > 0;
        
        if (!wasGrounded && newGroundedState)
        {
            float fallDistance = lastAirY - transform.position.y;
            if (fallDistance > maxSafeFallDistance)
            {
                Die();
            }
        }
    }

    void HandleMovement()
    {
        // 基于摄像机方向计算移动方向
        Vector3 cameraForward = mainCamera.transform.forward;
        Vector3 cameraRight = mainCamera.transform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 movement = (cameraForward * verticalInput) + (cameraRight * horizontalInput);

        if (movement.magnitude > 1f)
        {
            movement.Normalize();
        }

        movement *= moveSpeed;
        movement.y = rb.linearVelocity.y;
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
        if (jumpInput && groundContactCount > 0 && !IsCarryingItem)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            groundContactCount = 0;
            groundContacts.Clear();
        }
    }

    void HandlePickup()
    {
        if (pickupInput)
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
                DropItem();
            }
        }
    }
    // 强制设置为空闲状态
    void ForceIdleState()
    {
        // 重置移动状态
        isMoving = false;
        
        // 更新动画
        if (antAnimator != null)
        {
            antAnimator.SetBool("IsMoving", false);
        }
    }

    //停止物理移动
    void StopMovement()
    {
        // 保持Y轴速度（重力），但停止水平移动
        Vector3 currentVelocity = rb.linearVelocity;
        rb.linearVelocity = new Vector3(0f, currentVelocity.y, 0f);
        
        // 确保动画状态正确
        ForceIdleState();
    }

    // 放下物品的方法，会放到玩家前面
    void DropItem()
    {
        if (carriedItem != null)
        {
            // 移除父级关系
            carriedItem.transform.SetParent(null);
            
            // 设置物品位置到玩家前面的dropPoint位置
            carriedItem.transform.position = dropPoint.position;
            carriedItem.transform.rotation = dropPoint.rotation;

            Rigidbody itemRb = carriedItem.GetComponent<Rigidbody>();
            if (itemRb)
            {
                itemRb.isKinematic = false;
                // 可选：给物品一个小的向前推力，让它更自然地落下
                itemRb.linearVelocity = Vector3.zero;
            }

            Debug.Log("Dropped: " + carriedItem.name + " at position: " + dropPoint.position);
            carriedItem = null;
        }
    }


    void OnCollisionEnter(Collision collision)
    {
        // 检测与地面的碰撞
        if (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Stove"))
        {
            // 只有当这个地面物体不在列表中时才增加计数
            if (!groundContacts.Contains(collision.gameObject))
            {
                groundContacts.Add(collision.gameObject);
                groundContactCount++;
                Debug.Log($"Ground contact entered. Total contacts: {groundContactCount}");
            }
        }

        // 检测与灶台的碰撞（特殊处理）
        if (collision.gameObject.CompareTag("Stove"))
        {
            StoveDangerZone stove = collision.gameObject.GetComponent<StoveDangerZone>();
            if (stove != null)
            {
                stove.OnPlayerEnter(this);
            }
        }
        // 检测与水的碰撞
        else if (collision.gameObject.CompareTag("Water"))
        {
            Die();
        }

    }
    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            if (!groundContacts.Contains(collision.gameObject))
            {
                groundContacts.Add(collision.gameObject);
                groundContactCount++;
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        // 检测离开地面
        if (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Stove"))
        {
            // 如果这个地面物体在列表中，移除它
            if (groundContacts.Contains(collision.gameObject))
            {
                groundContacts.Remove(collision.gameObject);
                groundContactCount--;
                Debug.Log($"Ground contact exited. Total contacts: {groundContactCount}");
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Pickup") || other.CompareTag("Sugar"))
        {
            nearbyItem = other.gameObject;
            Debug.Log("Nearby item: " + nearbyItem.name);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if ((other.CompareTag("Pickup") || other.CompareTag("Sugar")) && other.gameObject == nearbyItem)
        {
            Debug.Log("Left item: " + other.name);
            nearbyItem = null;
        }
    }

    public void Die()
    {
        Debug.Log("Player died!");
        // 重生前先放下物品
        DropItem();
        rb.linearVelocity = Vector3.zero; // 重置速度
        // 重置地面接触
        groundContacts.Clear();
        groundContactCount = 0;

        // 使用存档点管理器获取最后一个激活的存档点位置
        Vector3 respawnPosition = CheckpointManager.Instance.GetLastRespawnPosition();
        transform.position = respawnPosition;
        
        // 可选：显示调试信息
        CheckpointManager.Instance.DebugActivatedCheckpoints();
        
        Debug.Log($"Respawned at last checkpoint: {respawnPosition}");
    }
}
