using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndLevel1 : MonoBehaviour
{
    [Header("Player Settings")]
    public GameObject player;
    
    [Header("Camera transition settings")]
    public Transform cameraEndPosition;    // The position where the camera has moved to (an empty object)
    public Transform cameraLookAtTarget;   // The position that the camera is pointing at (an empty object)
    public float transitionDuration = 2f;  // Camera movement time

    [Header("Animation control")]
    public GameObject endAnimationObject;  // Drag the End object with Animator attached here
    public GameObject nextAnimationObject;  

    [Header("Level settings")]
    public string nextLevelName = "Level2"; // The name of the scene for the next level
    public float totalSequenceTime = 4f;   // The total time of the entire ending sequence (camera movement + animation playback)
    
    [Header("Fade Out Settings")]
    public Image blackFadePanel;           // 黑色淡出面板
    public float fadeOutDuration = 2f;     // 淡出持续时间
    public float fadeOutDelay = 1f;        // 开始淡出前的延迟时间

    private PlayerInputController playerInputController;
    private CameraFollow cameraFollow;
    private Animator endAnimator;          // The Animator component of the End object
    private Animator nextAnimator;
    private bool hasTriggered = false; // Prevent repeated triggering

    void Start()
    {
        // If the player object has not been manually specified, attempt to automatically search for it.
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }

        if (player != null)
        {
            playerInputController = player.GetComponent<PlayerInputController>();
            cameraFollow = Camera.main.GetComponent<CameraFollow>();
            
            if (playerInputController == null)
            {
                Debug.LogWarning("[EndLevel1] Cannot find PlayerInputController component");
            }
            
            if (cameraFollow == null)
            {
                Debug.LogError("The CameraFollow component cannot be found on the main camera.");
            }
        }
        else
        {
            Debug.LogError("Cannot find the object with the Player tag");
        }

        // 获取End物体的Animator组件
        if (endAnimationObject != null)
        {
            endAnimator = endAnimationObject.GetComponent<Animator>();
            if (endAnimator == null)
            {
                Debug.LogError("The Animator component cannot be found on the End object.");
            }
        }
        else
        {
            Debug.LogError("need set End Animation Object");
        }
        if (nextAnimationObject != null)
        {
            nextAnimator = nextAnimationObject.GetComponent<Animator>();
            if (nextAnimator == null)
            {
                Debug.LogError("The Animator component cannot be found on the next animation object.");
            }
        }

        // 初始化黑色面板（确保开始时是隐藏的）
        if (blackFadePanel != null)
        {
            Color color = blackFadePanel.color;
            color.a = 0f;
            blackFadePanel.color = color;
            blackFadePanel.gameObject.SetActive(false);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if it was triggered by the player and has not been triggered before.
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true;
            EndLevelSequence();
        }
    }

    void EndLevelSequence()
    {
        Debug.Log("Start the level ending sequence. Total duration: " + totalSequenceTime + "seconds");
        
        // Set the IsEnd parameter of the animation to true
        if (endAnimator != null)
        {
            endAnimator.SetBool("IsEnd", true);
        }
        
        // Disable player input and camera follow-up
        if (playerInputController != null)
        {
            playerInputController.DisableInput();
        }
        
        if (cameraFollow != null)
        {
            cameraFollow.SetCameraControl(false);
        }
        
        // Start the end sequence coroutine
        StartCoroutine(EndSequenceCoroutine());
    }

    IEnumerator EndSequenceCoroutine()
    {
        // Record start time
        float sequenceStartTime = Time.time;

        // If there is a camera transition setting, execute the camera movement
        if (cameraEndPosition != null && cameraLookAtTarget != null)
        {
            yield return StartCoroutine(CameraTransition());
        }
        else
        {
            // If there is no camera cut, simply wait for the camera movement time to pass.
            yield return new WaitForSeconds(transitionDuration);
        }

        // Calculate the remaining time that needs to be waited.
        float elapsedTime = Time.time - sequenceStartTime;
        float remainingTime = totalSequenceTime - elapsedTime;

        if (endAnimator != null && endAnimationObject != null)
        {
            // 方法1：等待动画状态播放完成
            yield return StartCoroutine(WaitForAnimationToFinish(endAnimator));

            Debug.Log("First animation finished playing. Hiding the first animation object.");
            endAnimationObject.SetActive(false);
        }

        if (nextAnimator != null)
        {
            Debug.Log("Triggering the second animation's IsEnd parameter...");
            nextAnimator.SetBool("IsEnd", true);
            yield return new WaitForSeconds(2f); // 可选：等待第二个动画播放 2 秒
        }

        // 在加载下一关前执行黑色淡出效果
        yield return StartCoroutine(FadeOutBlackScreen());

        Debug.Log("Sequence completed. Loading next level.");
        LoadNextLevel();
    }

    // 新增方法：等待动画播放完成
    IEnumerator WaitForAnimationToFinish(Animator animator)
    {
        // 等待一帧确保动画状态已更新
        yield return null;

        // 获取当前播放的动画状态信息
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        // 等待动画播放完成
        while (stateInfo.normalizedTime < 1.0f)
        {
            yield return null;
            stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        }

        Debug.Log("Animation finished playing.");
    }

    IEnumerator CameraTransition()
    {
        float timer = 0f;
        Vector3 startPosition = cameraFollow.transform.position;
        Quaternion startRotation = cameraFollow.transform.rotation;

        // Calculate target rotation: Look towards the target position
        Vector3 lookDirection = cameraLookAtTarget.position - cameraEndPosition.position;
        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);

        while (timer < transitionDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / transitionDuration);

            // Smoothly move the position
            cameraFollow.transform.position = Vector3.Lerp(startPosition, cameraEndPosition.position, t);
            
            // Smooth rotation of the view
            cameraFollow.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            yield return null;
        }

        // Ensure the final position and rotation are accurate
        cameraFollow.transform.position = cameraEndPosition.position;
        cameraFollow.transform.rotation = targetRotation;

        Debug.Log("Camera transition completed");
    }

    IEnumerator FadeOutBlackScreen()
    {
        if (blackFadePanel == null)
        {
            Debug.LogWarning("BlackFadePanel is not assigned!");
            yield break;
        }

        // 延迟一段时间再开始淡出
        if (fadeOutDelay > 0)
        {
            yield return new WaitForSeconds(fadeOutDelay);
        }

        // 激活黑色面板
        blackFadePanel.gameObject.SetActive(true);

        float timer = 0f;
        Color color = blackFadePanel.color;
        float startAlpha = color.a; // 应该是0

        while (timer < fadeOutDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / fadeOutDuration);
            
            // 从透明渐变到不透明（黑色）
            color.a = Mathf.Lerp(startAlpha, 1f, t);
            blackFadePanel.color = color;
            
            yield return null;
        }

        // 确保最终完全不透明
        color.a = 1f;
        blackFadePanel.color = color;

        Debug.Log("Fade out completed");
    }

    void LoadNextLevel()
    {
        Debug.Log("Load the next level: 使用场景顺序跳转");
        
        // Load by using the scene order
        SceneOrderManager.Instance.LoadNextScene();
    }

    // 可选：添加一个公共方法用于在其他地方触发淡出
    public void TriggerFadeOut()
    {
        StartCoroutine(FadeOutBlackScreen());
    }
}