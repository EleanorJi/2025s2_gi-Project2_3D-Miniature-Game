using UnityEngine;

public class EndLevel1 : MonoBehaviour
{
    [Header("玩家设置")]
    public GameObject player; // 拖拽玩家对象到这里

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
        
        // 这里可以添加其他结束关卡的逻辑
        // 比如播放动画、显示UI等
    }
}