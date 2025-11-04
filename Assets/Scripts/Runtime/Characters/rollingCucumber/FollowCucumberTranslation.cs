using UnityEngine;

public class FollowCucumberTranslation : MonoBehaviour
{
    [Header("Follow settings")]
    public Transform cucumberTarget;  // cucumber object
    
    [Header("collider settings")]
    public BoxCollider leftBottomCollider;    // The bottom collision body on the left
    public BoxCollider rightBottomCollider;   // The bottom collision body on the right
    
    private rollingCucumber cucumberScript;
    private bool wasMovingLeft = true;
    private Vector3 leftColliderInitialOffset;
    private Vector3 rightColliderInitialOffset;
    private Quaternion parentInitialRotation;

    void Start()
    {
        if (cucumberTarget != null)
        {
            cucumberScript = cucumberTarget.GetComponent<rollingCucumber>();
            
            // Record the initial rotation of the parent object
            parentInitialRotation = transform.rotation;
            
            // Record separately the initial offset (in local coordinates) of each collision body relative to the cucumber.
            if (leftBottomCollider != null)
            {
                leftColliderInitialOffset = leftBottomCollider.transform.position - cucumberTarget.position;
            }
            
            if (rightBottomCollider != null)
            {
                rightColliderInitialOffset = rightBottomCollider.transform.position - cucumberTarget.position;
            }
        }
        
        if (cucumberScript == null)
        {
            Debug.LogError("The rollingCucumber script cannot be found.");
            return;
        }
        
        // Initial setting of the collision body state
        UpdateColliders(cucumberScript.IsMovingLeft);
        wasMovingLeft = cucumberScript.IsMovingLeft;
    }

    void LateUpdate()
    {
        if (cucumberScript == null || cucumberTarget == null) return;
        
        // Obtain the current movement direction of the cucumber
        bool isMovingLeft = cucumberScript.IsMovingLeft;
        
        // If the direction changes, update the collision body.
        if (isMovingLeft != wasMovingLeft)
        {
            UpdateColliders(isMovingLeft);
            wasMovingLeft = isMovingLeft;
        }
        
        // Update the position and rotation of the collision body
        UpdateColliderPositions();
    }

    void UpdateColliders(bool movingLeft)
    {
        if (leftBottomCollider != null)
            leftBottomCollider.enabled = movingLeft;
        
        if (rightBottomCollider != null)
            rightBottomCollider.enabled = !movingLeft;
    }

    void UpdateColliderPositions()
    {
        if (cucumberTarget == null) return;
        
        // Update the positions of the collision bodies, maintaining their original relative positions, and rotate in accordance with the parent object.
        if (leftBottomCollider != null)
        {
            leftBottomCollider.transform.position = cucumberTarget.position + leftColliderInitialOffset;
            // Maintain the same rotation as the parent object
            leftBottomCollider.transform.rotation = transform.rotation;
        }
        
        if (rightBottomCollider != null)
        {
            rightBottomCollider.transform.position = cucumberTarget.position + rightColliderInitialOffset;
            // Maintain the same rotation as the parent object
            rightBottomCollider.transform.rotation = transform.rotation;
        }
    }
}