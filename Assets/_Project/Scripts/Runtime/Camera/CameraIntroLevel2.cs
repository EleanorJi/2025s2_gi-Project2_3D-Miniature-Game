using UnityEngine;
using System.Collections;

public class CameraIntroLevel2 : MonoBehaviour
{
    [Header("Level2 Opening Animation Points")]
    public Transform startPoint;           // 起始点
    public Transform endPoint;             // 结束点
    public Transform lookAtTarget;         // 朝向目标点

    [Header("Player Settings")]
    public GameObject playerObject;        // 要隐藏的玩家物体（拖拽到这里）

    [Header("Animation Settings")]
    public Animator targetAnimator;        // target物体的Animator组件
    public string animationTrigger = "IsLevel2"; // 动画触发器名称
    public GameObject targetObject;        // 要隐藏的target物体

    [Header("Time Settings")]
    public float moveTime = 1.5f;          // 移动时间
    public float stayTime = 1f;            // 停留时间

    private CameraFollow cameraFollow;
    private PlayerInputController playerInputController;
    private bool isIntroPlaying = false;

    public bool IsIntroPlaying => isIntroPlaying;

    void Start()
    {
        cameraFollow = GetComponent<CameraFollow>();
        if (cameraFollow == null)
        {
            Debug.LogError("CameraIntroLevel2 need CameraFollow component!");
            return;
        }

        playerInputController = FindAnyObjectByType<PlayerInputController>();
        if (playerInputController == null)
        {
            Debug.LogWarning("PlayerInputController not found!");
            return;
        }

        // 检查路径点是否设置
        if (startPoint != null && endPoint != null && lookAtTarget != null)
        {
            StartCoroutine(PlayIntroAnimation());
        }
        else
        {
            Debug.LogWarning("Missing path points, no Level2 opening animation");
            // 如果没有设置点，直接启用玩家控制
            cameraFollow.SetCameraControl(true);
            if (playerInputController != null)
                playerInputController.EnableInput();
        }
    }

    public IEnumerator PlayIntroAnimation()
    {
        isIntroPlaying = true;
        
        // 隐藏玩家物体
        if (playerObject != null)
            playerObject.SetActive(false);

        // 触发target的动画
        if (targetAnimator != null && !string.IsNullOrEmpty(animationTrigger))
        {
            targetAnimator.SetTrigger(animationTrigger);
        }

        // 禁用玩家输入和相机跟随
        playerInputController.DisableInput();
        cameraFollow.SetCameraControl(false);

        // 设置起始位置和朝向（立即设置，没有过渡时间）
        transform.position = startPoint.position;
        
        // 立即看向目标点（没有旋转过渡时间）
        if (lookAtTarget != null)
        {
            Vector3 direction = lookAtTarget.position - transform.position;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
        
        // 移动到结束点（移动过程中持续看向target）
        yield return StartCoroutine(MoveToEndPoint());
        
        // 停留一会儿
        yield return new WaitForSeconds(stayTime);
        
        // 平滑过渡到玩家视角
        yield return StartCoroutine(SmoothTransitionToPlayer());

        // 开场动画结束：隐藏target物体并显示Player
        if (targetObject != null)
            targetObject.SetActive(false);
            
        if (playerObject != null)
            playerObject.SetActive(true);

        // 重新启用玩家输入和相机跟随
        cameraFollow.SetCameraControl(true);
        playerInputController.EnableInput();
        
        isIntroPlaying = false;
    }

    IEnumerator MoveToEndPoint()
    {
        float timer = 0f;
        Vector3 startPos = transform.position;

        while (timer < moveTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / moveTime);

            // 移动位置
            transform.position = Vector3.Lerp(startPos, endPoint.position, t);

            // 持续看向目标点（每帧更新，即使target移动也会跟随）
            if (lookAtTarget != null)
            {
                Vector3 currentDirection = lookAtTarget.position - transform.position;
                if (currentDirection != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(currentDirection);
                }
            }

            yield return null;
        }

        // 确保最终位置准确
        transform.position = endPoint.position;
        
        // 最终再确认一次朝向
        if (lookAtTarget != null)
        {
            Vector3 finalDirection = lookAtTarget.position - transform.position;
            if (finalDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(finalDirection);
            }
        }
    }

    IEnumerator SmoothTransitionToPlayer()
    {
        float transitionDuration = 1f;
        float timer = 0f;
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;

        Transform target = cameraFollow.Target;
        if (target == null) yield break;

        while (timer < transitionDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / transitionDuration);

            // 计算目标位置和旋转（使用CameraFollow的偏移）
            Quaternion targetRot = Quaternion.Euler(cameraFollow.Pitch, cameraFollow.Yaw, 0);
            Vector3 targetPos = target.position + targetRot * cameraFollow.Offset;

            transform.position = Vector3.Lerp(startPosition, targetPos, t);

            // 平滑看向玩家
            Vector3 lookDir = target.position - transform.position;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(startRotation, Quaternion.LookRotation(lookDir), t);
            }
            yield return null;
        }
    }
}