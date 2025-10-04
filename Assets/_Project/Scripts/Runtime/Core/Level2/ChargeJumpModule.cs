using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Rigidbody))]
public class ChargeJumpModule : MonoBehaviour
{
    [Header("输入")]
    public KeyCode jumpKey = KeyCode.C;         // 蓄力键（避免与 Space 冲突）

    [Header("落地检测")]
    public Transform groundCheck;               // 放脚底
    public float groundRadius = 0.18f;
    public LayerMask groundMask;                // 勾 Ground | Rock

    [Header("蓄力参数（只影响距离）")]
    public float chargeRate = 8f;               // 每秒增加的“charge”
    public float maxCharge = 12f;               // charge 上限

    [Header("力度拆分：高度固定，距离随蓄力")]
    public float verticalImpulse = 4.0f;        // 固定起跳高度（不随蓄力变）
    public float baseHorizontal = 2.0f;         // 不蓄力也会有的水平冲量
    public float horizontalPerCharge = 1.0f;    // 每 1 点 charge 增加的水平冲量

    [Header("方向 & 手感")]
    public float airControlMultiplier = 0.2f;   // 空中微调强度
    public float maxAirSpeed = 8f;              // 空中水平最大速度

    [Header("仅在岩石上可蓄力")]
    public bool requireOnRock = true;           // 只踩 Rock 才允许蓄力
    public string rockTag = "Rock";

    [Header("教程UI（可选）")]
    public CanvasGroup hintGroup;
    public TMP_Text hintText;
    public Image chargeBar;                    // Type=Filled Horizontal

    [Header("由区域开启/关闭")]
    public bool chargeEnabled = false;         // 由 ChargeJumpZone 控制

    Rigidbody rb;
    float charge;
    bool charging;
    Vector3 chargeDir = Vector3.forward;       // 锁定的起跳方向（地面投影的蚂蚁 forward）

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!groundCheck) {
            var t = new GameObject("GroundCheck").transform;
            t.SetParent(transform);
            t.localPosition = new Vector3(0, -0.5f, 0);
            groundCheck = t;
        }
        SetHint(false);
    }

    public void SetChargeEnabled(bool on)
    {
        chargeEnabled = on;
        SetHint(on);
    }

    void Update()
    {
        bool grounded = Physics.CheckSphere(groundCheck.position, groundRadius, groundMask);
        bool allowChargeNow = (!requireOnRock) || OnRock();

        // UI
        if (hintGroup) hintGroup.alpha = chargeEnabled ? 1f : 0f;
        if (hintText)  hintText.text  = "Hold <b>C</b> to charge, release to jump farther.\nNo W needed.";
        if (chargeBar) chargeBar.fillAmount = Mathf.Clamp01(charge / maxCharge);

        if (!chargeEnabled) { StopCharge(); return; }

        // 开始蓄力：锁定方向（用相机的水平朝向），还可顺手把蚂蚁转过去
        if (Input.GetKeyDown(jumpKey) && grounded && allowChargeNow)
        {
            charging = true;
            charge = 0f;
        
            // 用相机水平朝向（如果没有相机引用，就退回用自身 forward）
            Vector3 fwd = Camera.main ? Camera.main.transform.forward : transform.forward;
            chargeDir = Vector3.ProjectOnPlane(fwd, Vector3.up).normalized;
            if (chargeDir.sqrMagnitude < 0.0001f) chargeDir = transform.forward;
        
            // 可选：把蚂蚁缓慢对准这个方向（1帧内瞬转也行）
            Quaternion look = Quaternion.LookRotation(chargeDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, 1f); // 1f=瞬转；改小点=平滑
        }


        // 按住蓄力
        if (charging && Input.GetKey(jumpKey))
        {
            charge += chargeRate * Time.deltaTime;
            charge = Mathf.Min(charge, maxCharge);
        }

        // 松开起跳
        if (charging && Input.GetKeyUp(jumpKey))
        {
            if (grounded && allowChargeNow) DoJump(charge, chargeDir);
            StopCharge();
        }

        // 空中微调（可选）
        if (!grounded) ApplyAirControl(chargeDir);
    }

    void StopCharge() { charging = false; charge = 0f; }

    bool OnRock()
    {
        var hits = Physics.OverlapSphere(groundCheck.position, groundRadius, groundMask);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].CompareTag(rockTag)) return true;
            if (hits[i].GetComponentInParent<RockSurface>() != null ||
                hits[i].GetComponent<RockSurface>() != null) return true;
        }
        return false;
    }

    // 最终：高度固定 + 水平随蓄力
    void DoJump(float finalCharge, Vector3 planarDir)
    {
        // 记录上一块岩石（供蜂蜜回退用）
        GetComponent<RockTracker>()?.MarkJump();

        // 清垂直速度，保留水平
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        // 距离 = 基础水平 + 蓄力 * 系数（可在此做非线性映射）
        float horiz = baseHorizontal + finalCharge * horizontalPerCharge;
        float vert  = verticalImpulse;

        Vector3 impulse = planarDir.normalized * horiz + Vector3.up * vert;
        rb.AddForce(impulse, ForceMode.Impulse);
    }

    void ApplyAirControl(Vector3 planarDir)
    {
        if (planarDir.sqrMagnitude < 0.0001f) return;
        Vector3 v = rb.linearVelocity;
        Vector3 pv = new Vector3(v.x, 0f, v.z);
        Vector3 wish = planarDir * maxAirSpeed;
        Vector3 add = (wish - pv) * airControlMultiplier * Time.deltaTime * 10f;
        rb.linearVelocity = new Vector3(pv.x + add.x, v.y, pv.z + add.z);
    }

    void SetHint(bool show)
    {
        if (!hintGroup) return;
        hintGroup.alpha = show ? 1f : 0f;
        hintGroup.blocksRaycasts = false;
        hintGroup.ignoreParentGroups = true;
    }
}
