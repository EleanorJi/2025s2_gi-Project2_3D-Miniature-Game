using UnityEngine;
using System.Collections;

public class CheckpointUp : MonoBehaviour
{
    [Header("玩家和蚂蚁动画")]
    public GameObject playerObject;           // 玩家物体（Ant）
    public GameObject antMoveObject;          // antMove物体（包含动画的）
    public string playerAnimationTrigger = "upLevel2";  // 玩家动画触发器名称
    public GameObject antsObject;             // 蚂蚁物体
    public string antsAnimationTrigger = "Activate";    // 蚂蚁动画触发器名称
    
    [Header("玩家位置设置")]
    public Transform playerTargetPosition;    // 玩家移动到的位置（动画起始位置）
    public Transform playerTargetRotation;    // 玩家面向的方向（可选）
    public Transform playerFinalPosition;     // 动画结束后玩家的最终位置
    
    [Header("相机转场设置")]
    public Transform cameraTargetPosition;    // 相机移动到的位置
    public Transform cameraLookAtTarget;      // 相机看向的位置
    public float transitionDuration = 2f;     // 转场时间
    
    [Header("动画设置")]
    public float animationDuration = 5f;      // 动画持续时间
    
    [Header("玩家输入控制")]
    private PlayerInputController playerInputController;
    private CameraFollow cameraFollow;
    private PlayerController playerController;
    // private GameObject playerObject;
    private bool playerInRange = false;
    private bool isAnimating = false;

    // Animator组件
    private Animator playerAnimator;          // antMove上的Animator
    private Animator antsAnimator;

    void Start()
    {
        playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerInputController = playerObject.GetComponent<PlayerInputController>();
            playerController = playerObject.GetComponent<PlayerController>();
            
            if (playerObject == null)
            {
                playerObject = playerObject;
            }
        }
        
        cameraFollow = Camera.main?.GetComponent<CameraFollow>();
        
        // 获取Animator组件
        if (antMoveObject != null)
        {
            playerAnimator = antMoveObject.GetComponent<Animator>();
            if (playerAnimator == null)
            {
                Debug.LogError("在antMove物体上找不到Animator组件！");
            }
        }
        else
        {
            Debug.LogError("请将antMove物体拖拽到antMoveObject字段中！");
        }
        
        if (antsObject != null)
        {
            antsAnimator = antsObject.GetComponent<Animator>();
        }
    }
    
    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E) && !isAnimating)
        {
            ActivateCheckpoint();
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isAnimating)
        {
            playerInRange = true;
            Debug.Log("玩家进入检查点区域，按E键激活");
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && !isAnimating)
        {
            playerInRange = false;
            Debug.Log("玩家离开检查点区域");
        }
    }
    
    void MovePlayerToTargetPosition()
    {
        if (playerObject != null && playerTargetPosition != null)
        {
            playerObject.transform.position = playerTargetPosition.position;
            playerObject.transform.forward = playerTargetPosition.forward;

            Debug.Log($"玩家已移动到动画起始位置: {playerTargetPosition.position}");
            Debug.Log($"玩家当前朝向: {playerObject.transform.forward}");
        }
    }
    
    void MovePlayerToFinalPosition()
    {
        if (playerObject != null && playerFinalPosition != null)
        {
            playerObject.transform.position = playerFinalPosition.position;
            Debug.Log($"玩家已移动到最终位置: {playerFinalPosition.position}");
        }
        else
        {
            Debug.LogError("玩家或最终位置未设置！");
        }
    }
    
    void TriggerAllAnimations()
    {
        if (playerAnimator != null && !string.IsNullOrEmpty(playerAnimationTrigger))
        {
            playerAnimator.SetTrigger(playerAnimationTrigger);
            Debug.Log($"触发玩家动画: {playerAnimationTrigger}");
        }
        
        if (antsAnimator != null && !string.IsNullOrEmpty(antsAnimationTrigger))
        {
            antsAnimator.SetTrigger(antsAnimationTrigger);
            Debug.Log($"触发蚂蚁动画: {antsAnimationTrigger}");
        }
    }
    
    IEnumerator CheckpointSequence()
    {
        yield return null;
        
        // 相机转场到目标位置
        if (cameraTargetPosition != null && cameraLookAtTarget != null)
        {
            yield return StartCoroutine(MoveCameraToTarget());
        }
        
        // 触发所有动画
        Debug.Log("开始播放所有动画");
        TriggerAllAnimations();
        
        // 等待动画播放完成
        yield return new WaitForSeconds(animationDuration);
        
        // 动画完成后将玩家移动到最终位置
        Debug.Log("动画播放完成，移动玩家到最终位置");
        MovePlayerToFinalPosition();
        
        // 恢复玩家控制
        RestorePlayerControl();
        
        // 返回相机到玩家视角
        if (cameraTargetPosition != null && cameraLookAtTarget != null)
        {
            yield return StartCoroutine(ReturnCameraToPlayer());
        }
        
        isAnimating = false;
        playerInRange = false;
        Debug.Log("检查点动画完成，玩家控制已恢复");
    }

    void ActivateCheckpoint()
    {
        if (isAnimating) return;
        
        isAnimating = true;
        Debug.Log("进入动画 - 检查点激活");
        
        // 禁用玩家输入、相机跟随和物理控制
        if (playerInputController != null) playerInputController.DisableInput();
        if (cameraFollow != null) cameraFollow.SetCameraControl(false);
        
        // 禁用 PlayerController
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        // 禁用Rigidbody物理，让动画完全控制位置
        Rigidbody playerRb = playerObject.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            playerRb.isKinematic = true;
        }

        // 移动玩家到动画起始位置
        MovePlayerToTargetPosition();
        
        // 启动相机转场序列
        StartCoroutine(CheckpointSequence());
    }

    void RestorePlayerControl()
    {
        // 先重新启用 PlayerController
        if (playerController != null)
        {
            playerController.enabled = true;
        }
        
        // 重新启用Rigidbody物理
        Rigidbody playerRb = playerObject.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            playerRb.isKinematic = false;
        }
        
        // 恢复玩家输入
        if (playerInputController != null)
        {
            playerInputController.EnableInput();
        }
        
        // 恢复相机控制
        if (cameraFollow != null)
        {
            cameraFollow.SetCameraControl(true);
        }
        
        Debug.Log("玩家控制已恢复，可以继续移动");
    }
    
    IEnumerator MoveCameraToTarget()
    {
        float timer = 0f;
        Vector3 startPosition = Camera.main.transform.position;
        Quaternion startRotation = Camera.main.transform.rotation;

        Vector3 lookDirection = cameraLookAtTarget.position - cameraTargetPosition.position;
        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);

        while (timer < transitionDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / transitionDuration);

            Camera.main.transform.position = Vector3.Lerp(startPosition, cameraTargetPosition.position, t);
            Camera.main.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            yield return null;
        }

        Camera.main.transform.position = cameraTargetPosition.position;
        Camera.main.transform.rotation = targetRotation;
    }
    
    IEnumerator ReturnCameraToPlayer()
    {
        float timer = 0f;
        Vector3 startPosition = Camera.main.transform.position;
        Quaternion startRotation = Camera.main.transform.rotation;

        if (cameraFollow != null)
        {
            cameraFollow.SetCameraControl(true);
        }

        // 简单的返回逻辑
        while (timer < transitionDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / transitionDuration);

            // 让CameraFollow逐渐接管控制
            yield return null;
        }
    }
}