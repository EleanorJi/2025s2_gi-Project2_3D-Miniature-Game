using UnityEngine;
using Antventure.UI;
using Antventure.UI.Menus;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    [Header("Movement and Jumping Parameters")]
    public float moveSpeed = 5f;
    public float jumpForce = 7f;
    private float startThreshold = 0.1f;
    private float stopThreshold = 0.3f;

    // Animation control
    private Animator antAnimator;
    private bool isMoving = false;
    private ParachuteCarrier parachuteCarrier;

    [Header("Ground detection parameters")]
    public int groundContactCount = 0;

    [Header("pickup parameters")]
    public Transform carryPoint;
    public Transform dropPoint;
    private GameObject carriedItem;
    private GameObject nearbyItem;

    [Header("Death and Rebirth Parameters")]
    public float maxSafeFallDistance = 5f;
    public Transform respawnPoint;

    // Component reference
    private Rigidbody rb;
    private Camera mainCamera;
    public bool IsCarryingItem => carriedItem != null;

    // Input cache
    private float horizontalInput;
    private float verticalInput;
    private bool jumpInput;
    private bool pickupInput;

    private List<GameObject> groundContacts = new List<GameObject>();
    private PlayerInputController inputController;

    public trafficlights trafficlights;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        mainCamera = Camera.main;
        antAnimator = GetComponentInChildren<Animator>();

        if (antAnimator == null)
        {
            Debug.LogError("Animator not found on ant model!");
        }

        // Ensure that the archive point manager is present
        if (CheckpointManager.Instance == null)
        {
            GameObject checkpointManager = new GameObject("CheckpointManager");
            checkpointManager.AddComponent<CheckpointManager>();
        }

        inputController = GetComponent<PlayerInputController>();
        if (inputController == null)
        {
            inputController = gameObject.AddComponent<PlayerInputController>();
        }

        parachuteCarrier = GetComponent<ParachuteCarrier>();
        if (parachuteCarrier == null)
        {
            Debug.Log("[PlayerController] No ParachuteCarrier component found - this is optional");
        }

        // Hide the cursor - use UnifiedCursorManager if available
        if (UnifiedCursorManager.Instance != null)
        {
            UnifiedCursorManager.Instance.ForceHideCursor();
        }
        else if (CursorManager.Instance != null)
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
        // Only hide cursor if pause menu is not open
        if (UnifiedCursorManager.Instance != null)
        {
            // Let UnifiedCursorManager handle cursor state
            if (!UnifiedCursorManager.Instance.IsPauseMenuOpen() && Cursor.visible)
            {
                Debug.LogWarning("[PLAYER] Cursor became visible during gameplay (pause not open), requesting hide");
                UnifiedCursorManager.Instance.ForceHideCursor();
            }
        }
        else
        {
            // Fallback behavior - only hide if no pause system is active
            if (Cursor.visible && (GamePauseSystem.Instance == null || !GamePauseSystem.Instance.IsPaused))
            {
                Debug.LogWarning("[PLAYER] Cursor became visible during gameplay, hiding it again");
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        if (inputController != null && !inputController.IsInputEnabled())
        {

            StopMovement();
            return;
        }

        // Read input
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");
        jumpInput = Input.GetKeyDown(KeyCode.Space);
        pickupInput = Input.GetKeyDown(KeyCode.C);

        // Update the movement status (for animation)
        UpdateMovementState();

        // Handling jumps and pickup
        HandleJump();
        HandlePickup();

        // Update animation parameters
        UpdateAnimationParameters();

        // Debugging: Pressing the R key results in death.
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

        // Handling movement
        HandleMovement();
    }

    void UpdateAnimationParameters()
    {
        if (antAnimator != null)
        {
            bool isCarryingSomething = IsCarryingItem ||
                                       (parachuteCarrier != null && parachuteCarrier.HasLeaf());

            antAnimator.SetBool("IsMoving", isMoving);
            antAnimator.SetBool("PickUp", isCarryingSomething);
        }
    }

    void UpdateMovementState()
    {
        // Update the movement status for animation
        if (isMoving)
        {
            isMoving = Mathf.Abs(horizontalInput) > stopThreshold || Mathf.Abs(verticalInput) > stopThreshold;
        }
        else
        {
            isMoving = Mathf.Abs(horizontalInput) > startThreshold || Mathf.Abs(verticalInput) > startThreshold;
        }
    }

    void HandleMovement()
    {
        // Calculate the movement direction based on the direction of the camera.
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

        // Make the character face the direction of movement.
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

                // update animation parameters immediately
                UpdateAnimationParameters();

                GlobalSfx.PlayLeafPickupSfx(transform.position);

                Debug.Log("Picked up: " + carriedItem.name);
            }
            else if (carriedItem != null)
            {
                DropItem();
            }
        }
    }

    // Set to idle state forcibly
    void ForceIdleState()
    {
        // reset movement
        isMoving = false;

        UpdateAnimationParameters();
    }

    //Stop physical movement
    void StopMovement()
    {
        // Maintain the Y-axis speed (gravity), but stop the horizontal movement
        Vector3 currentVelocity = rb.linearVelocity;
        rb.linearVelocity = new Vector3(0f, currentVelocity.y, 0f);

        // Ensure that the animation state is correct
        ForceIdleState();
    }

    // The method of placing the items will be shown to the player on the screen.
    void DropItem()
    {
        if (carriedItem != null)
        {
            // Remove parent-child relationship
            carriedItem.transform.SetParent(null);

            // Set the item's position to the dropPoint location in front of the player.
            carriedItem.transform.position = dropPoint.position;
            carriedItem.transform.rotation = dropPoint.rotation;

            Rigidbody itemRb = carriedItem.GetComponent<Rigidbody>();
            if (itemRb)
            {
                itemRb.isKinematic = false;
                // Give the object a small forward push to make it fall more naturally.
                itemRb.linearVelocity = Vector3.zero;
            }


            UpdateAnimationParameters();

            GlobalSfx.PlayLeafPickupSfx(transform.position);

            Debug.Log("Dropped: " + carriedItem.name + " at position: " + dropPoint.position);
            carriedItem = null;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Detecting collisions with the ground
        if (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Stove"))
        {
            // Only when this ground object is not on the list will the count be incremented.
            if (!groundContacts.Contains(collision.gameObject))
            {
                groundContacts.Add(collision.gameObject);
                groundContactCount++;
                Debug.Log($"Ground contact entered. Total contacts: {groundContactCount}");
            }
        }

        // Detecting collisions with the Stove
        if (collision.gameObject.CompareTag("Stove"))
        {
            StoveDangerZone stove = collision.gameObject.GetComponent<StoveDangerZone>();
            if (stove != null)
            {
                stove.OnPlayerEnter(this);
            }
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
        // Detecting collisions exit the ground
        if (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Stove"))
        {
            // If this ground object is present in the list, remove it.
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

        if (other.CompareTag("feather"))
        {
            var rend = this.GetComponentInChildren<Renderer>();
            if (rend != null)
                StartCoroutine(FlashRed(rend, 0.1f));
        }

        if (other.CompareTag("Traffic"))
        {
            Debug.Log("??????????????");
            trafficlights.enabled = true;
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
        
        // ★★★ 首先触发PlayerHealth的死亡事件，这会显示死亡UI ★★★
        PlayerHealth playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            // 设置健康值为0来触发死亡事件
            playerHealth.TakeDamage("PlayerController.Die()", playerHealth.CurrentHealth);
            return; // PlayerHealth.Respawn()会被死亡UI调用，不需要在这里处理重生
        }
        
        // 如果没有PlayerHealth组件，则尝试用全局/本地死亡UI做兜底显示
        try
        {
            if (Antventure.UI.GlobalDeathUIController.Instance != null)
            {
                Antventure.UI.GlobalDeathUIController.Instance.ShowDeathUI();
            }
            else if (DeathUIOverlay.Instance != null)
            {
                DeathUIOverlay.Instance.Show();
            }
        }
        catch { /* 防御性兜底，不影响后续重生 */ }

        // 然后使用原来的逻辑
        PerformDirectRespawn();
    }
    
    /// <summary>
    /// 直接重生逻辑（当没有PlayerHealth组件时使用）
    /// </summary>
    public void PerformDirectRespawn()
    {
        Debug.Log("Performing direct respawn (no PlayerHealth component)");
        
        // dropItem before die
        DropItem();

        // ★★★ 死亡时清掉所有小兵 ★★★
        MinionAnchor.KillAll();

        // reset physical state
        rb.linearVelocity = Vector3.zero;

        // reset ground count
        groundContacts.Clear();
        groundContactCount = 0;

        // get checkpoint
        Vector3 respawnPosition = CheckpointManager.Instance.GetLastRespawnPosition();
        transform.position = respawnPosition;

        // reset position
        transform.rotation = Quaternion.identity;

        // debug（防御空引用，以免场景切换后旧引用导致异常）
        try
        {
            if (CheckpointManager.Instance != null)
                CheckpointManager.Instance.DebugActivatedCheckpoints();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[PlayerController] DebugActivatedCheckpoints failed: {ex.Message}");
        }

        Debug.Log($"Respawned at last checkpoint: {respawnPosition}");
    }

    public void RefreshAnimationState()
    {
        UpdateAnimationParameters();
    }

    IEnumerator FlashRed(Renderer renderer, float duration)
    {
        if (renderer == null)
            yield break;

        renderer.material.color = Color.red;
        yield return new WaitForSeconds(duration);
        renderer.material.color = Color.white;
    }
}
