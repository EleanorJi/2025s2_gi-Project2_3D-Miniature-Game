using UnityEngine;

public class FollowCucumberTranslation : MonoBehaviour
{
    [Header("跟随设置")]
    public Transform cucumberTarget;  // 拖入黄瓜物体到这里
    
    [Header("碰撞体设置")]
    public BoxCollider leftBottomCollider;    // 左边的底部碰撞体
    public BoxCollider rightBottomCollider;   // 右边的底部碰撞体
    
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
            
            // 记录父物体的初始旋转
            parentInitialRotation = transform.rotation;
            
            // 分别记录每个碰撞体相对于黄瓜的初始偏移（本地坐标）
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
            Debug.LogError("无法找到rollingCucumber脚本");
            return;
        }
        
        // 初始设置碰撞体状态
        UpdateColliders(cucumberScript.IsMovingLeft);
        wasMovingLeft = cucumberScript.IsMovingLeft;
    }

    void LateUpdate()
    {
        if (cucumberScript == null || cucumberTarget == null) return;
        
        // 获取黄瓜当前移动方向
        bool isMovingLeft = cucumberScript.IsMovingLeft;
        
        // 如果方向改变，更新碰撞体
        if (isMovingLeft != wasMovingLeft)
        {
            UpdateColliders(isMovingLeft);
            wasMovingLeft = isMovingLeft;
        }
        
        // 更新碰撞体位置和旋转
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
        
        // 更新碰撞体位置，保持各自的初始相对位置，旋转与父物体保持一致
        if (leftBottomCollider != null)
        {
            leftBottomCollider.transform.position = cucumberTarget.position + leftColliderInitialOffset;
            // 保持与父物体相同的旋转
            leftBottomCollider.transform.rotation = transform.rotation;
        }
        
        if (rightBottomCollider != null)
        {
            rightBottomCollider.transform.position = cucumberTarget.position + rightColliderInitialOffset;
            // 保持与父物体相同的旋转
            rightBottomCollider.transform.rotation = transform.rotation;
        }
    }
}