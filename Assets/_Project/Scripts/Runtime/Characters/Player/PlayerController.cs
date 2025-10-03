using UnityEngine;
using Antventure.UI;

public class PlayerController : MonoBehaviour
{
    [Header("移动与跳跃参数")]
    // 移动速度
    public float moveSpeed = 5f;
    // 跳跃力度
    public float jumpForce = 7f;
    private float startThreshold = 0.1f;
    private float stopThreshold = 0.3f;
    // 动画控制
    private Animator antAnimator;
    private bool isMoving = false;

    [Header("地面检测参数")]
    public int groundContactCount = 0; // 记录与地面的接触数量

    [Header("拾取参数")]
    public Transform carryPoint;   // 背上挂载点
    public Transform dropPoint;    // 放下物品的位置参考点
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
    // 是否拿着物品
    public bool IsCarryingItem => carriedItem != null;

    // 存储当前接触的地面物体
    private System.Collections.Generic.List<GameObject> groundContacts = new System.Collections.Generic.List<GameObject>();

    private PlayerInputController inputController;

    void Start()
    {
        // 获取组件
        rb = GetComponent<Rigidbody>();
        mainCamera = Camera.main; // 获取主摄像机

        // 获取子物体（蚂蚁模型）上的Animator组件
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
        else
        {
            Debug.Log("PlayerInputController found successfully");
        }
        
        // Hide cursor during gameplay (third-person camera control)
        Debug.Log("[PLAYER] Attempting to hide cursor for gameplay");
        if (CursorManager.Instance != null)
        {
            Debug.Log("[PLAYER] CursorManager found, calling HideCursor()");
            CursorManager.Instance.HideCursor();
        }
        else
        {
            Debug.LogWarning("[PLAYER] CursorManager.Instance is null! Using fallback cursor hiding");
            // Fallback if CursorManager is not available
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        
    }

    void Update()
    {
        // Ensure cursor stays hidden during gameplay
        if (Cursor.visible)
        {
            Debug.LogWarning("[PLAYER] Cursor became visible during gameplay, hiding it again");
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        if (inputController != null && !inputController.IsInputEnabled())
        {
            Debug.Log("输入被禁用，跳过玩家输入处理");
            return;
        }
        
        // 更新地面状态
        UpdateGroundState();
        
        // 处理跳跃输入
        HandleJump();
        HandlePickup();

        // 检测移动输入
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        // 如果正在移动，检查是否应该停止；如果停止，检查是否应该开始
        if (isMoving)
        {
            // 只有当输入很小的时候才停止
            isMoving = Mathf.Abs(horizontalInput) > stopThreshold || Mathf.Abs(verticalInput) > stopThreshold;
        }
        else
        {
            // 只要有轻微输入就开始
            isMoving = Mathf.Abs(horizontalInput) > startThreshold || Mathf.Abs(verticalInput) > startThreshold;
        }

        // 控制动画
        if (antAnimator != null)
        {
            antAnimator.SetBool("IsMoving", isMoving);
            // Debug.Log($"IsMoving: {isMoving}, Horizontal: {horizontalInput}, Vertical: {verticalInput}");
        }

        // 记录离开地面瞬间的高度
        if (!isGrounded && wasGrounded)
        {
            lastAirY = transform.position.y;
        }
        wasGrounded = isGrounded;

        // 调试：按R键显示当前激活的存档点
        if (Input.GetKeyDown(KeyCode.R))
        {
            Die();
        }
    }

    void FixedUpdate()
    {
        if (inputController != null && !inputController.IsInputEnabled())
        {
            Debug.Log("输入被禁用，跳过玩家输入处理");
            return;
        }
        // 处理移动
        HandleMovement();
    }

    void UpdateGroundState()
    {
        // 只要有至少一个地面接触，就认为在地面上
        bool newGroundedState = groundContactCount > 0;
        
        // 如果刚刚落地，检测下落伤害
        if (!isGrounded && newGroundedState)
        {
            float fallDistance = lastAirY - transform.position.y;
            if (fallDistance > maxSafeFallDistance)
            {
                Die();
            }
        }
        
        isGrounded = newGroundedState;
    }

    void HandleMovement()
    {
        if (inputController != null && !inputController.IsInputEnabled())
        {
            Debug.Log("输入被禁用，跳过玩家输入处理");
            return;
        }
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
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !IsCarryingItem)
        {
            // 应用向上的力来实现跳跃
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            // 跳跃后立即设置为不在地面，防止连续跳跃
            isGrounded = false;
            groundContactCount = 0;
            groundContacts.Clear();
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
                DropItem();
            }
        }
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
