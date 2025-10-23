using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CameraIntroLevel2 : MonoBehaviour
{
    [Header("Level2 Opening Animation Points")]
    public Transform startPoint;           // 起始点
    public Transform endPoint;             // 结束点
    public Transform lookAtTarget;         // 朝向目标点

    [Header("Player Settings")]
    public GameObject playerObject;        // 要隐藏的玩家物体

    [Header("Animation Settings")]
    public Animator targetAnimator;        // target物体的Animator组件
    public string animationTrigger = "IsLevel2"; // 动画触发器名称
    public GameObject targetObject;        // 要隐藏的target物体

    [Header("Time Settings")]
    public float moveTime = 1.5f;          // 移动时间
    public float stayTime = 1f;            // 停留时间
    public float fadeInTime = 1f;          // 黑色淡入时间
    public float fadeInDelay = 0f;         // 开始淡入前的延迟时间

    [Header("Fade Settings")]
    public Image blackFadeImage;           // 黑色UI面板

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

        // 初始化黑色面板
        if (blackFadeImage != null)
        {
            // 开始时设置为完全不透明（全黑）
            Color startColor = blackFadeImage.color;
            startColor.a = 1f;
            blackFadeImage.color = startColor;
            blackFadeImage.gameObject.SetActive(true);
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
            StartCoroutine(FadeInOnly());
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

        // 设置起始位置和朝向
        transform.position = startPoint.position;
        
        // 立即看向目标点
        if (lookAtTarget != null)
        {
            Vector3 direction = lookAtTarget.position - transform.position;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        // 同时开始淡入效果和移动动画
        yield return StartCoroutine(PlayFadeAndMoveSimultaneously());
        
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

    IEnumerator PlayFadeAndMoveSimultaneously()
    {
        // 同时启动淡入和移动协程
        Coroutine fadeCoroutine = StartCoroutine(FadeInBlackScreen());
        Coroutine moveCoroutine = StartCoroutine(MoveToEndPoint());

        // 等待两个动画都完成
        yield return fadeCoroutine;
        yield return moveCoroutine;
    }

    IEnumerator FadeInBlackScreen()
    {
        if (blackFadeImage == null) yield break;

        // 延迟一段时间再开始淡入
        if (fadeInDelay > 0)
        {
            yield return new WaitForSeconds(fadeInDelay);
        }

        float timer = 0f;
        Color color = blackFadeImage.color;
        float startAlpha = color.a; // 应该是1

        while (timer < fadeInTime)
        {
            timer += Time.deltaTime;
            float t = timer / fadeInTime; // 使用线性插值，与EndLevel1保持一致
            
            // 从不透明渐变到透明（黑色消失）
            color.a = Mathf.Lerp(startAlpha, 0f, t);
            blackFadeImage.color = color;
            
            yield return null;
        }

        // 确保最终完全透明
        color.a = 0f;
        blackFadeImage.color = color;
        
        // 完全透明后禁用UI以提升性能
        blackFadeImage.gameObject.SetActive(false);
    }

    IEnumerator FadeInOnly()
    {
        // 仅执行淡入效果，然后启用控制
        yield return StartCoroutine(FadeInBlackScreen());
        
        cameraFollow.SetCameraControl(true);
        if (playerInputController != null)
            playerInputController.EnableInput();
    }

    IEnumerator MoveToEndPoint()
    {
        float timer = 0f;
        Vector3 startPos = transform.position;

        while (timer < moveTime)
        {
            timer += Time.deltaTime;
            float t = timer / moveTime; // 使用线性插值，与EndLevel1保持一致

            transform.position = Vector3.Lerp(startPos, endPoint.position, t);

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

        transform.position = endPoint.position;
        
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
            float t = timer / transitionDuration; // 使用线性插值

            Quaternion targetRot = Quaternion.Euler(cameraFollow.Pitch, cameraFollow.Yaw, 0);
            Vector3 targetPos = target.position + targetRot * cameraFollow.Offset;

            transform.position = Vector3.Lerp(startPosition, targetPos, t);

            Vector3 lookDir = target.position - transform.position;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(startRotation, Quaternion.LookRotation(lookDir), t);
            }
            yield return null;
        }
    }

    // 可选：添加一个公共方法用于其他地方的淡出效果（与EndLevel1保持一致）
    public IEnumerator FadeOutBlackScreen(float duration = 1f, float delay = 0f)
    {
        if (blackFadeImage == null) yield break;

        // 延迟
        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }

        blackFadeImage.gameObject.SetActive(true);
        
        float timer = 0f;
        Color color = blackFadeImage.color;
        float startAlpha = color.a;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            
            color.a = Mathf.Lerp(startAlpha, 1f, t);
            blackFadeImage.color = color;
            
            yield return null;
        }

        color.a = 1f;
        blackFadeImage.color = color;
    }
}