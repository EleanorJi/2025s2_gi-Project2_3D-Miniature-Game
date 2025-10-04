using UnityEngine;
using System.Collections;

public class EndLevel1 : MonoBehaviour
{
    [Header("玩家设置")]
    public GameObject player; // 拖拽玩家对象到这里

    [Header("相机转场设置")]
    public Transform cameraEndPosition;    // 相机移动到的位置（空物体）
    public Transform cameraLookAtTarget;   // 相机看向的位置（空物体）
    public float transitionDuration = 2f;  // 转场时间

    private PlayerInputController playerInputController;
    private CameraFollow cameraFollow;
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
        Debug.Log("播放结束动画");
        
        // 禁用玩家输入和相机跟随
        if (playerInputController != null)
        {
            playerInputController.DisableInput();
        }
        
        if (cameraFollow != null)
        {
            cameraFollow.SetCameraControl(false);
        }
        
        // 启动相机转场协程
        if (cameraEndPosition != null && cameraLookAtTarget != null)
        {
            StartCoroutine(CameraTransition());
        }
        else
        {
            Debug.LogWarning("相机转场目标位置未设置，跳过转场动画");
        }
        
        // 这里可以添加其他结束关卡的逻辑
        // 比如播放动画、显示UI等
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
        
        // 这里可以继续播放其他结束动画或显示UI
    }
}