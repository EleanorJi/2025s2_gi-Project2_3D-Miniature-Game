using UnityEngine;
using Antventure.UI;

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
    private float lastAirY;
    private bool wasGrounded;

    // Component reference
    private Rigidbody rb;
    private Camera mainCamera;
    public bool IsCarryingItem => carriedItem != null;

    // Input cache
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

        // Ensure that the archive point manager is present
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

        // Hide the cursor
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
        // Make sure the cursor is hidden
        if (Cursor.visible)
        {
            Debug.LogWarning("[PLAYER] Cursor became visible during gameplay, hiding it again");
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        if (inputController != null && !inputController.IsInputEnabled())
        {
            // Debug.Log("Input is disabled. Skip the processing of player input.");
            // Control animation
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

        // Update the ground status
        UpdateGroundState();

        // Handling jumps and pickup
        HandleJump();
        HandlePickup();

        // Record the height at the moment of leaving the ground
        if (!(groundContactCount > 0) && wasGrounded)
        {
            lastAirY = transform.position.y;
        }
        wasGrounded = groundContactCount > 0;

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

        // Control animation
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

        // update animation
        if (antAnimator != null)
        {
            antAnimator.SetBool("IsMoving", false);
        }
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
        // Detecting collisions with the Water
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
        // dropItem before die
        DropItem();

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

        // debug
        CheckpointManager.Instance.DebugActivatedCheckpoints();

        Debug.Log($"Respawned at last checkpoint: {respawnPosition}");
    }

}
