using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class StickyGooMashR : MonoBehaviour
{
    [Header("规则")]
    public int requiredPresses = 15;     // 需要按R次数
    public float timeLimit = 8f;         // 倒计时（秒）
    public int instantDeathOnNth = 3;    // 第N次落入直接死亡

    [Header("UI（圆环+竖条）")]
    public CanvasGroup ringGroup;        // 面板整体（CanvasGroup）
    public Image ringFill;               // 圆环 Image（Type=Filled, Radial360）
    public Image timerFill;              // 竖条 Image（Type=Filled, Vertical, Origin=Top）
    public TMP_Text tipText;             // 文本（可空）

    [Header("回到哪")]
    public float upOffset = 0.8f;        // 回石头时上抬
    public Transform smallLevelStart;    // 本小关起始石头的 RespawnAnchor（第一块石头）

    [Header("冻结方式")]
    public bool setKinematicWhileStuck = true;
    public bool zeroVelocityWhileStuck = true;

    private bool busy;
    private int stuckTimes = 0;          // 累计落入次数（跨本局）
    private PlayerController pc;
    private RockTracker tracker;
    private Rigidbody prb;
    private float cachedSpeed;
    private bool cachedKinematic;

    void Reset() { GetComponent<Collider>().isTrigger = true; }

    void OnTriggerEnter(Collider other)
    {
        if (busy) return;
        if (!other.CompareTag("Player")) return;

        pc = other.GetComponent<PlayerController>();
        tracker = other.GetComponent<RockTracker>();
        prb = other.GetComponent<Rigidbody>();
        if (!pc || !prb) return;

        stuckTimes++;
        if (instantDeathOnNth > 0 && stuckTimes >= instantDeathOnNth) {
            FailToStart(); return;
        }

        StartCoroutine(MashRoutine());
    }

    IEnumerator MashRoutine()
    {
        busy = true;
        // 冻结
        cachedSpeed = pc.moveSpeed; pc.moveSpeed = 0f;
        cachedKinematic = prb.isKinematic; if (setKinematicWhileStuck) prb.isKinematic = true;
        if (zeroVelocityWhileStuck) { prb.linearVelocity = Vector3.zero; prb.angularVelocity = Vector3.zero; }

        // UI 出现
        if (ringGroup) ringGroup.alpha = 1f;
        if (ringFill)  ringFill.fillAmount = 0f;
        if (timerFill) timerFill.fillAmount = 1f;
        if (tipText)   tipText.text = "MASH R TO ESCAPE";

        int presses = 0;
        float remain = timeLimit;

        while (remain > 0f && presses < requiredPresses)
        {
            remain -= Time.deltaTime;

            if (Input.GetKeyDown(KeyCode.R)) {
                presses++;
                if (ringFill) ringFill.fillAmount = (float)presses / requiredPresses;
            }
            if (timerFill) timerFill.fillAmount = Mathf.Clamp01(remain / timeLimit);

            if (zeroVelocityWhileStuck && !setKinematicWhileStuck)
                prb.linearVelocity = Vector3.zero;

            yield return null;
        }

        // UI 收起
        if (ringGroup) ringGroup.alpha = 0f;
        if (ringFill)  ringFill.fillAmount = 0f;

        // 成功 or 失败
        if (presses >= requiredPresses) SucceedToLastRock();
        else FailToStart();

        // 解冻
        if (setKinematicWhileStuck) prb.isKinematic = cachedKinematic;
        pc.moveSpeed = cachedSpeed;
        busy = false;
    }

    void SucceedToLastRock()
    {
        // 回“起跳时的上一块石头”
        Vector3 target = prb.position;
        Transform anchor = null;

        var rock = tracker ? tracker.lastJumpFromRock : null;
        if (rock && rock.respawnAnchor) anchor = rock.respawnAnchor;
        else if (rock) { // 无锚点则用碰撞体顶面
            var col = rock.GetComponentInChildren<Collider>();
            if (col) target = col.bounds.center + Vector3.up * (col.bounds.extents.y + upOffset);
        }

        if (anchor) target = anchor.position + Vector3.up * 0.02f;

        // 传送
        bool temp = prb.isKinematic; prb.isKinematic = true;
        prb.position = target; prb.linearVelocity = Vector3.zero; prb.angularVelocity = Vector3.zero;
        prb.isKinematic = temp;
    }

    void FailToStart()
    {
        // 失败/第N次：回本小关起点或用你的 Die()
        if (smallLevelStart) {
            bool temp = prb.isKinematic; prb.isKinematic = true;
            prb.position = smallLevelStart.position + Vector3.up * 0.02f;
            prb.linearVelocity = Vector3.zero; prb.angularVelocity = Vector3.zero;
            prb.isKinematic = temp;
            // 也可在这儿触发“死亡动画/GIF”
        } else {
            pc.Die();
        }
    }
}
