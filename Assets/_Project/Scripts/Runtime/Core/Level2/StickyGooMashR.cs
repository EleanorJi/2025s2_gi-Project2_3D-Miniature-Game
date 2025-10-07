using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class StickyGooMashR : MonoBehaviour
{
    [Header("规则")]
    public int requiredPresses = 15;
    public float timeLimit = 8f;

    [Header("第 N 次进入蜂蜜直接死亡")]
    public int instantDeathOnNth = 3;

    [Header("计数范围")]
    public bool useGlobalCounter = true;

    [Header("UI（挣脱进度圈）")]
    public CanvasGroup ringGroup;
    public Image ringFill;   // Filled/Radial360
    public Image timerFill;  // Filled/Vertical Origin=Top
    public TMP_Text tipText;

    [Header("成功自救：回上一块石头")]
    public float upOffset = 0.8f;
    public Transform smallLevelStart = null; // 留空：失败/第N次走 pc.Die()

    [Header("按键")]
    public KeyCode mashKey = KeyCode.J;

    [Header("冻结")]
    public bool setKinematicWhileStuck = true;
    public bool zeroVelocityWhileStuck = true;

    [Header("死亡 UI（仅在真正死亡时显示）")]
    [TextArea]
    public string deathMessage = "被蜂蜜困住，挣脱失败…";
    public Sprite deathSprite;
    public float deathDuration = 4f; // <=0 使用模板默认

    // —— 内部 ——
    bool busy;
    PlayerController pc;
    RockTracker tracker;
    Rigidbody rb;
    float cachedSpeed;
    bool cachedKinematic;

    int localTimes = 0;
    static int globalTimes = 0;

    void Reset() { GetComponent<Collider>().isTrigger = true; }
    void Awake() { ShowUI(false); }
    void OnEnable(){ ShowUI(false); }

    // 外部（如重生点触发器）可调用
    public static void ResetGlobalHoneyCounter() { globalTimes = 0; }
    public void ResetLocalHoneyCounter() { localTimes = 0; }

    int  Cnt()      => useGlobalCounter ? globalTimes : localTimes;
    void SetCnt(int v){ if (useGlobalCounter) globalTimes = v; else localTimes = v; }
    int  Inc()      { int v = Cnt() + 1; SetCnt(v); return v; }

    void OnTriggerEnter(Collider other)
    {
        if (busy) return;

        var root = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform;
        if (!root.CompareTag("Player")) return;

        pc      = root.GetComponent<PlayerController>();
        tracker = root.GetComponent<RockTracker>();
        rb      = root.GetComponent<Rigidbody>();
        if (!pc || !rb) return;

        // 若还没“上一块”而此刻踩在石头上，补一次
        if (tracker && tracker.lastJumpFromRock == null && tracker.currentRock != null)
            tracker.MarkJump();

        int times = Inc(); // 1,2,3...
        if (instantDeathOnNth > 0 && times >= instantDeathOnNth)
        {
            Die(true);
            return;
        }

        StartCoroutine(MashRoutine());
    }

    IEnumerator MashRoutine()
    {
        busy = true;

        // 冻结
        cachedSpeed = pc.moveSpeed; pc.moveSpeed = 0f;
        cachedKinematic = rb.isKinematic;
        if (setKinematicWhileStuck) rb.isKinematic = true;
        if (zeroVelocityWhileStuck) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }

        // UI
        if (tipText) tipText.text = $"MASH {mashKey} TO ESCAPE";
        if (ringFill) ringFill.fillAmount = 0f;
        if (timerFill) timerFill.fillAmount = 1f;
        ShowUI(true);

        int presses = 0; float t = timeLimit;
        while (t > 0f && presses < requiredPresses)
        {
            t -= Time.deltaTime;
            if (Input.GetKeyDown(mashKey))
            {
                presses++;
                if (ringFill) ringFill.fillAmount = (float)presses / requiredPresses;
            }
            if (timerFill) timerFill.fillAmount = Mathf.Clamp01(t / timeLimit);

            if (zeroVelocityWhileStuck && !setKinematicWhileStuck)
                rb.linearVelocity = Vector3.zero;

            yield return null;
        }

        ShowUI(false);

        if (presses >= requiredPresses) TeleportToLastRock();
        else                            Die(true);

        // 解冻
        if (setKinematicWhileStuck) rb.isKinematic = cachedKinematic;
        pc.moveSpeed = cachedSpeed;
        busy = false;
    }

    void TeleportToLastRock()
    {
        var rock = tracker ? tracker.lastJumpFromRock : null;
        if (rock == null) return;

        Vector3 target = rb.position;
        if (rock.respawnAnchor)
            target = rock.respawnAnchor.position + Vector3.up * 0.02f;
        else
        {
            var col = rock.GetComponentInChildren<Collider>();
            if (col) target = col.bounds.center + Vector3.up * (col.bounds.extents.y + upOffset);
        }

        bool keepK = rb.isKinematic; rb.isKinematic = true;
        rb.position = target;
        rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
        rb.isKinematic = keepK;
    }

    void Die(bool resetCounter)
    {
        ShowUI(false);

        if (smallLevelStart)
        {
            // 不是“真正死亡”——只是传送回小重生点，不显示死亡UI
            bool keepK = rb.isKinematic; rb.isKinematic = true;
            rb.position = smallLevelStart.position + Vector3.up * 0.02f;
            rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
            rb.isKinematic = keepK;
        }
        else
        {
            // 真正死亡：走全局死亡流程 + 弹死亡UI模板
            pc.Die();

            // 只有此处触发死亡UI；可在组件里自定义文案/图片/时长
            DeathUIOverlay.Instance?.Show(
                string.IsNullOrEmpty(deathMessage) ? null : deathMessage,
                deathSprite,
                (deathDuration > 0f) ? deathDuration : (float?)null
            );
        }

        if (resetCounter) SetCnt(0); // 死亡后重新给两次机会
    }

    void ShowUI(bool show)
    {
        if (!ringGroup) return;
        ringGroup.alpha = show ? 1f : 0f;
        ringGroup.blocksRaycasts = false;
        ringGroup.interactable = false;
    }
}
