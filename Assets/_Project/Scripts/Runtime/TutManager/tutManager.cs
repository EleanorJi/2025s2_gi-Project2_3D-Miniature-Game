using UnityEngine;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    public Transform player;           // 拖 ant_col（父节点，带刚体/碰撞）
    public Rigidbody playerRb;         // 可留空，Start 里自动 GetComponent
    public Transform spawnPoint;       // 书柜上的出生点（可选）
    public TextMeshProUGUI prompt;     // 画布上的提示文本

    [Header("流程触发（可选：用触发器更稳）")]
    public TutorialGate moveGate;      // 放在书排后面，玩家穿过去算通过移动
    public TutorialGate jumpGate;      // 放在障碍物后面，玩家跳过去穿到这个门算通过跳跃

    [Header("阈值（不放触发器时使用）")]
    public float moveDistanceRequired = 0.6f;  // 水平移动多少米算通过移动步骤
    public float minAirborneTime = 0.1f;       // 离地最少时间算“跳起来了”
    public float groundRayLen = 0.06f;         // Raycast 贴地检测长度（米）

    [Header("提示文本")]
    [TextArea] public string moveTip = "用 WASD 移动，绕过前面那排书。";
    [TextArea] public string jumpTip = "按 空格 跳跃，越过前面的障碍。";
    [TextArea] public string doneTip = "干得好！教程完成～";

    Vector3 startPosXZ;
    float airborneTimer;
    bool wasGrounded;

    public enum Step { Move, Jump, Done }
    public Step step = Step.Move;

    void Start()
    {
        if (!player) { Debug.LogError("TutorialManager: 请把 player 拖到脚本上"); enabled = false; return; }
        if (!playerRb) playerRb = player.GetComponent<Rigidbody>();

        if (spawnPoint) player.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);

        // 记录初始水平位置
        startPosXZ = new Vector3(player.position.x, 0, player.position.z);

        // 触发器回调（可选）
        if (moveGate) moveGate.Init(this, Step.Move);
        if (jumpGate) jumpGate.Init(this, Step.Jump);

        Show(moveTip);
    }

    void Update()
    {
        switch (step)
        {
            case Step.Move:
                // 方案1：放了 moveGate，用门决定通过
                // 方案2：没放门，就用移动距离
                if (!moveGate)
                {
                    Vector3 nowXZ = new Vector3(player.position.x, 0, player.position.z);
                    if (Vector3.Distance(nowXZ, startPosXZ) >= moveDistanceRequired)
                    {
                        GoJumpStep();
                    }
                }
                break;

            case Step.Jump:
                // 如果没有 jumpGate，就用“离地一段时间再落地”的方式判断跳跃成功
                if (!jumpGate)
                {
                    bool grounded = IsGrounded();
                    if (!grounded) airborneTimer += Time.deltaTime;
                    if (grounded && wasGrounded == false && airborneTimer >= minAirborneTime)
                    {
                        GoDoneStep();
                    }
                    wasGrounded = grounded;
                }
                break;

            case Step.Done:
                // 可以在这里触发下一关/关闭 UI 等
                break;
        }
    }

    // 供 TutorialGate 调用
    public void CompleteStepFromGate(Step gateFor)
    {
        if (step == gateFor)
        {
            if (gateFor == Step.Move) GoJumpStep();
            else if (gateFor == Step.Jump) GoDoneStep();
        }
    }

    void GoJumpStep()
    {
        step = Step.Jump;
        airborneTimer = 0f;
        wasGrounded = IsGrounded();
        Show(jumpTip);
    }

    void GoDoneStep()
    {
        step = Step.Done;
        Show(doneTip);
        // 可选：几秒后隐藏
        // StartCoroutine(HideAfter(2f));
    }

    bool IsGrounded()
    {
        // 简单贴地检测：从玩家中心向下打一条短射线，命中 Ground 层就算在地面
        Vector3 origin = player.position + Vector3.up * 0.02f;
        return Physics.Raycast(origin, Vector3.down, groundRayLen, LayerMask.GetMask("Ground"), QueryTriggerInteraction.Ignore);
    }

    void Show(string s)
    {
        if (prompt) prompt.text = s;
        else Debug.Log("[Tutorial] " + s);
    }
}
