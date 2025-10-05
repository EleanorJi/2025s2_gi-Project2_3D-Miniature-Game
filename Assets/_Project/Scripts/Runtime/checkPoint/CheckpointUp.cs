using UnityEngine;
using System.Collections;

public class CheckpointUp : MonoBehaviour
{
    [Header("玩家对象")]
    public GameObject playerObject;           // 玩家物体（真实的蚂蚁）
    public GameObject antsAnimationObject;    // 播放动画的蚂蚁（替身）
    public string antsAnimationTrigger = "Activate";    // 动画触发器

    [Header("玩家位置设置")]
    public Transform playerFinalPosition;     // 动画结束后玩家的最终位置

    [Header("相机设置")]
    public Transform cameraTargetPosition;    
    public Transform cameraLookAtTarget;      
    public float transitionDuration = 2f;

    [Header("动画设置")]
    public float animationDuration = 3f;

    private bool playerInRange = false;
    private bool isAnimating = false;

    private PlayerInputController playerInputController;
    private PlayerController playerController;
    private CameraFollow cameraFollow;
    private Animator antsAnimator;

    void Start()
    {
        // 找玩家
        playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerInputController = playerObject.GetComponent<PlayerInputController>();
            playerController = playerObject.GetComponent<PlayerController>();
        }

        // 找相机
        cameraFollow = Camera.main?.GetComponent<CameraFollow>();

        // 找动画替身
        if (antsAnimationObject != null)
        {
            antsAnimator = antsAnimationObject.GetComponent<Animator>();
            // 🔹 游戏开始时先隐藏替身蚂蚁
            antsAnimationObject.SetActive(false);
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

    void ActivateCheckpoint()
    {
        if (isAnimating) return;
        isAnimating = true;
        Debug.Log("检查点激活，开始动画");

        // 禁用玩家控制
        if (playerInputController != null) playerInputController.DisableInput();
        if (playerController != null) playerController.enabled = false;
        if (cameraFollow != null) cameraFollow.SetCameraControl(false);

        // 暂时隐藏真实玩家蚂蚁
        if (playerObject != null && antsAnimationObject != null)
        {
            playerObject.SetActive(false);
            antsAnimationObject.SetActive(true);
        }

        // 播放替身动画
            if (antsAnimator != null && !string.IsNullOrEmpty(antsAnimationTrigger))
            {
                antsAnimator.SetTrigger(antsAnimationTrigger);
                Debug.Log($"触发替身动画: {antsAnimationTrigger}");
            }

        StartCoroutine(CheckpointSequence());
    }

    IEnumerator CheckpointSequence()
    {
        // 相机转场
        if (cameraTargetPosition != null && cameraLookAtTarget != null)
            yield return StartCoroutine(MoveCameraToTarget());

        // 等待动画播放完毕
        yield return new WaitForSeconds(animationDuration);

        Debug.Log("动画播放完成，恢复真实玩家");
        // 🔹 动画播放结束后隐藏替身蚂蚁
        if (antsAnimationObject != null)
            antsAnimationObject.SetActive(false);
        // 显示真实蚂蚁并移动到最终位置
        if (playerObject != null && playerFinalPosition != null)
        {
            playerObject.transform.position = playerFinalPosition.position;
            playerObject.transform.forward = playerFinalPosition.forward;
            playerObject.SetActive(true);
        }

        // 恢复控制
        if (playerInputController != null) playerInputController.EnableInput();
        if (playerController != null) playerController.enabled = true;
        if (cameraFollow != null) cameraFollow.SetCameraControl(true);

        isAnimating = false;
        playerInRange = false;
        Debug.Log("检查点完成，玩家恢复控制");
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
}
