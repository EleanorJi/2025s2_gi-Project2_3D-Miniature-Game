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

        // 先执行黑色淡入效果
        yield return StartCoroutine(FadeInBlackScreen());

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
        
        // 移动到结束点
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

    IEnumerator FadeInBlackScreen()
    {
        if (blackFadeImage == null) yield break;

        float timer = 0f;
        Color color = blackFadeImage.color;
        float startAlpha = color.a;

        while (timer < fadeInTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / fadeInTime);
            
            color.a = Mathf.Lerp(startAlpha, 0f, t);
            blackFadeImage.color = color;
            
            yield return null;
        }

        // 完全透明后禁用UI以提升性能
        color.a = 0f;
        blackFadeImage.color = color;
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
            float t = Mathf.SmoothStep(0f, 1f, timer / moveTime);

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
            float t = Mathf.SmoothStep(0f, 1f, timer / transitionDuration);

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

    // 可选：添加一个公共方法用于其他地方的淡出效果
    public IEnumerator FadeOutBlackScreen(float duration = 1f)
    {
        if (blackFadeImage == null) yield break;

        blackFadeImage.gameObject.SetActive(true);
        
        float timer = 0f;
        Color color = blackFadeImage.color;
        float startAlpha = color.a;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / duration);
            
            color.a = Mathf.Lerp(startAlpha, 1f, t);
            blackFadeImage.color = color;
            
            yield return null;
        }

        color.a = 1f;
        blackFadeImage.color = color;
    }
}