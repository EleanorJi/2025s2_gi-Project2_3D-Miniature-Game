using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class EndLevel1 : MonoBehaviour
{
    [Header("玩家设置")]
    public GameObject player; // 拖拽玩家对象到这里

    [Header("相机转场设置")]
    public Transform cameraEndPosition;    // 相机移动到的位置（空物体）
    public Transform cameraLookAtTarget;   // 相机看向的位置（空物体）
    public float transitionDuration = 2f;  // 相机移动时间

    [Header("动画控制")]
    public GameObject endAnimationObject;  // 拖拽带有Animator的End物体到这里

    [Header("关卡设置")]
    public string nextLevelName = "Level2"; // 下一关的场景名称
    public float totalSequenceTime = 4f;   // 整个结束序列的总时间（相机移动+动画播放）

    private PlayerInputController playerInputController;
    private CameraFollow cameraFollow;
    private Animator endAnimator;          // End物体的Animator组件
    private bool hasTriggered = false; // 防止重复触发

    void Start()
    {
        // 如果未手动指定玩家对象，尝试自动查找
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
                Debug.LogError("在玩家对象上找不到 PlayerInputController 组件");
            }
            
            if (cameraFollow == null)
            {
                Debug.LogError("在主摄像机上找不到 CameraFollow 组件");
            }
        }
        else
        {
            Debug.LogError("找不到带有 Player 标签的对象");
        }

        // 获取End物体的Animator组件
        if (endAnimationObject != null)
        {
            endAnimator = endAnimationObject.GetComponent<Animator>();
            if (endAnimator == null)
            {
                Debug.LogError("在End物体上找不到Animator组件");
            }
        }
        else
        {
            Debug.LogError("请设置End Animation Object");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // 检查是否是玩家触发且尚未触发过
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true;
            EndLevelSequence();
        }
    }

    void EndLevelSequence()
    {
        Debug.Log("开始关卡结束序列，总时长: " + totalSequenceTime + "秒");
        
        // 设置动画的IsEnd参数为true
        if (endAnimator != null)
        {
            endAnimator.SetBool("IsEnd", true);
        }
        
        // 禁用玩家输入和相机跟随
        if (playerInputController != null)
        {
            playerInputController.DisableInput();
        }
        
        if (cameraFollow != null)
        {
            cameraFollow.SetCameraControl(false);
        }
        
        // 启动结束序列协程
        StartCoroutine(EndSequenceCoroutine());
    }

    IEnumerator EndSequenceCoroutine()
    {
        // 记录开始时间
        float sequenceStartTime = Time.time;
        
        // 如果有相机转场设置，执行相机移动
        if (cameraEndPosition != null && cameraLookAtTarget != null)
        {
            yield return StartCoroutine(CameraTransition());
        }
        else
        {
            Debug.LogWarning("相机转场目标位置未设置，跳过转场动画");
            // 如果没有相机转场，直接等待相机移动时间
            yield return new WaitForSeconds(transitionDuration);
        }

        // 计算剩余需要等待的时间
        float elapsedTime = Time.time - sequenceStartTime;
        float remainingTime = totalSequenceTime - elapsedTime;

        // 如果还有剩余时间，等待动画播放完毕
        if (remainingTime > 0)
        {
            Debug.Log("等待动画播放完成，剩余时间: " + remainingTime.ToString("F2") + "秒");
            yield return new WaitForSeconds(remainingTime);
        }
        else
        {
            Debug.LogWarning("总时间设置可能过短，立即加载下一关");
        }

        Debug.Log("结束序列完成，加载下一关");
        LoadNextLevel();
    }

    IEnumerator CameraTransition()
    {
        float timer = 0f;
        Vector3 startPosition = cameraFollow.transform.position;
        Quaternion startRotation = cameraFollow.transform.rotation;

        // 计算目标旋转：看向目标位置
        Vector3 lookDirection = cameraLookAtTarget.position - cameraEndPosition.position;
        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);

        while (timer < transitionDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / transitionDuration);

            // 平滑移动位置
            cameraFollow.transform.position = Vector3.Lerp(startPosition, cameraEndPosition.position, t);
            
            // 平滑旋转视角
            cameraFollow.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            yield return null;
        }

        // 确保最终位置和旋转准确
        cameraFollow.transform.position = cameraEndPosition.position;
        cameraFollow.transform.rotation = targetRotation;

        Debug.Log("相机转场完成");
    }

    void LoadNextLevel()
    {
        Debug.Log("加载下一关: " + nextLevelName);
        
        // 使用场景名称加载
        SceneManager.LoadScene(nextLevelName);
        
        // 如果需要使用Build Index，取消注释下面的代码
        // int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        // SceneManager.LoadScene(currentSceneIndex + 1);
    }
}