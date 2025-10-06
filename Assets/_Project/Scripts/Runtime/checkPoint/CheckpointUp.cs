using UnityEngine;
using System.Collections;
using TMPro;

public class CheckpointUp : MonoBehaviour
{
    [Header("玩家对象")]
    public GameObject playerObject;           
    public GameObject antsAnimationObject;    
    public string antsAnimationTrigger = "Activate";

    [Header("玩家位置设置")]
    public Transform playerFinalPosition;     

    [Header("相机设置")]
    public Transform cameraTargetPosition;    
    public Transform cameraLookAtTarget;      
    public float transitionDuration = 2f;

    [Header("动画设置")]
    public float animationDuration = 3f;

    [Header("攀爬前置条件（饼干）")]
    public int requiredCookies = 3;                // 需要的饼干数
    public bool consumeOnClimb = true;             // 够了是否立刻扣除
    public FloodSequence flood;                    // 同一段洪水脚本（成功后让它停）

    [Header("提示UI（不足时）")]
    public CanvasGroup hintGroup;                  // 可用你现有的提示面板
    public TMP_Text hintText;
    public string notEnoughText = "能量不足（需要 3 个饼干碎屑）";
    public float hintFadeTime = 0.2f;
    public float hintStayTime = 1.2f;

    private bool playerInRange = false;
    private bool isAnimating = false;

    private PlayerInputController playerInputController;
    private PlayerController playerController;
    private CameraFollow cameraFollow;
    private Animator antsAnimator;
    private Coroutine hintCo;

    void Start()
    {
        // 找玩家
        playerObject = playerObject ? playerObject : GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerInputController = playerObject.GetComponent<PlayerInputController>();
            playerController = playerObject.GetComponent<PlayerController>();
        }

        // 找相机
        cameraFollow = Camera.main ? Camera.main.GetComponent<CameraFollow>() : null;

        // 动画替身
        if (antsAnimationObject != null)
        {
            antsAnimator = antsAnimationObject.GetComponent<Animator>();
            antsAnimationObject.SetActive(false);
        }

        if (hintGroup) hintGroup.alpha = 0f;
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E) && !isAnimating)
        {
            TryActivate();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isAnimating)
        {
            playerInRange = true;
            // 这里可选：显示“按E攀爬”的提示
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && !isAnimating)
        {
            playerInRange = false;
            HideHintImmediate();
        }
    }

    // —— 先做饼干判定 —— //
    void TryActivate()
    {
        var inv = CookiesInventory.Instance;
        int have = inv ? inv.cookies : 0;

        if (inv == null)
        {
            Debug.LogWarning("[CheckpointUp] CookiesInventory.Instance 为空，跳过判定（调试中视为通过）");
            ActivateCheckpoint();
            return;
        }

        if (have < requiredCookies)
        {
            ShowHint(notEnoughText);
            return; // 不播放动画
        }

        // 够了：按需扣除
        if (consumeOnClimb)
        {
            bool ok = inv.Spend(requiredCookies);
            if (!ok) { ShowHint(notEnoughText); return; } // 理论上不会
        }

        ActivateCheckpoint();
    }

    // —— 原有流程：禁操作→替身动画→相机过渡→落点复位 —— //
    void ActivateCheckpoint()
    {
        if (isAnimating) return;
        isAnimating = true;

        if (playerInputController) playerInputController.DisableInput();
        if (playerController)      playerController.enabled = false;
        if (cameraFollow)          cameraFollow.SetCameraControl(false);

        if (playerObject && antsAnimationObject)
        {
            playerObject.SetActive(false);
            antsAnimationObject.SetActive(true);
        }

        if (antsAnimator && !string.IsNullOrEmpty(antsAnimationTrigger))
            antsAnimator.SetTrigger(antsAnimationTrigger);

        StartCoroutine(CheckpointSequence());
    }

    IEnumerator CheckpointSequence()
    {
        // 相机转场（可选）
        if (cameraTargetPosition && cameraLookAtTarget)
            yield return StartCoroutine(MoveCameraToTarget());

        // 等动画
        yield return new WaitForSeconds(animationDuration);

        // 结束替身，复位玩家
        if (antsAnimationObject) antsAnimationObject.SetActive(false);

        if (playerObject && playerFinalPosition)
        {
            playerObject.transform.SetPositionAndRotation(
                playerFinalPosition.position, playerFinalPosition.rotation);
            playerObject.SetActive(true);
        }

        // 恢复控制
        if (playerInputController) playerInputController.EnableInput();
        if (playerController)      playerController.enabled = true;
        if (cameraFollow)          cameraFollow.SetCameraControl(true);

        // 通知洪水“已过关”，让它停止并复原
        if (flood) flood.OnClimbSucceeded(resetCrumbs: false); // 我们已扣过饼干，不再清零

        isAnimating = false;
        playerInRange = false;
        HideHintImmediate();
    }

    IEnumerator MoveCameraToTarget()
    {
        float timer = 0f;
        var cam = Camera.main;
        if (!cam) yield break;

        Vector3 startPos = cam.transform.position;
        Quaternion startRot = cam.transform.rotation;

        Vector3 lookDir = cameraLookAtTarget.position - cameraTargetPosition.position;
        Quaternion targetRot = Quaternion.LookRotation(lookDir);

        while (timer < transitionDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / transitionDuration);
            cam.transform.position = Vector3.Lerp(startPos, cameraTargetPosition.position, t);
            cam.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }
        cam.transform.SetPositionAndRotation(cameraTargetPosition.position, targetRot);
    }

    // —— 简单提示 —— //
    void ShowHint(string msg)
    {
        if (!hintGroup || !hintText) { Debug.Log(msg); return; }
        if (hintCo != null) StopCoroutine(hintCo);
        hintCo = StartCoroutine(HintRoutine(msg));
    }
    void HideHintImmediate()
    {
        if (!hintGroup) return;
        if (hintCo != null) StopCoroutine(hintCo);
        hintGroup.alpha = 0f;
    }
    IEnumerator HintRoutine(string msg)
    {
        hintText.text = msg;
        for (float t=0; t<hintFadeTime; t+=Time.unscaledDeltaTime)
        { hintGroup.alpha = Mathf.Lerp(0,1,t/hintFadeTime); yield return null; }
        hintGroup.alpha = 1f;
        yield return new WaitForSecondsRealtime(hintStayTime);
        for (float t=0; t<hintFadeTime; t+=Time.unscaledDeltaTime)
        { hintGroup.alpha = Mathf.Lerp(1,0,t/hintFadeTime); yield return null; }
        hintGroup.alpha = 0f;
    }
}
