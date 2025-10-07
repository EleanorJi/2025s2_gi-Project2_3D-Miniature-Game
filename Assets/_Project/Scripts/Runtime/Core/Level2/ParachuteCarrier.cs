using UnityEngine;

/// <summary>
/// 挂在 Player 上：按 C 拾取附近的叶子，保持原尺寸背到 carryPoint；
/// 通过 Gate 传送；落地后第一次移动自动从 dropPoint 丢下。
/// 朝向规则：背在身上时 X=-90, Z=90；丢下时在当前姿态基础上 X 再 +90°。
/// </summary>
public class ParachuteCarrier : MonoBehaviour
{
    [Header("输入")]
    public KeyCode pickupKey = KeyCode.C;

    [Header("引用（自动从 PlayerController 获取）")]
    public Transform carryPoint;
    public Transform dropPoint;

    [Header("背在身上的姿态（可在 Inspector 微调）")]
    public Vector3 attachLocalEuler = new Vector3(-90f, 0f, 90f);
    public Vector3 attachLocalOffset = Vector3.zero;

    [Header("丢下时位置微调")]
    public float dropYOffset = 0.03f; // 放下时世界坐标向上抬高

    // 状态
    ParachuteLeafPickup nearbyLeaf;   // 可拾取范围标记
    Transform carriedLeaf;            // 正在背的叶子
    Rigidbody carriedLeafRb;
    Collider[] carriedLeafCols;

    // 落地->第一次移动 丢叶
    PlayerController pc;
    bool wasGroundedLastFrame;
    bool waitingForFirstMoveAfterLand;

    void Awake()
    {
        pc = GetComponent<PlayerController>();
        if (pc)
        {
            if (!carryPoint) carryPoint = pc.carryPoint;
            if (!dropPoint)  dropPoint  = pc.dropPoint;
        }
        if (!carryPoint) Debug.LogWarning("[ParachuteCarrier] 缺少 carryPoint");
        if (!dropPoint)  Debug.LogWarning("[ParachuteCarrier] 缺少 dropPoint");
    }

    void Update()
    {
        // 按 C 拾取
        if (Input.GetKeyDown(pickupKey))
        {
            if (!carriedLeaf && nearbyLeaf)
                AttachLeaf(nearbyLeaf.transform);
        }

        // 落地 -> 等待第一次移动 就丢叶
        bool groundedNow = pc ? (pc.groundContactCount > 0) : false;
        if (!wasGroundedLastFrame && groundedNow)
            waitingForFirstMoveAfterLand = carriedLeaf != null;

        if (waitingForFirstMoveAfterLand && carriedLeaf)
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
            {
                DropLeaf();
                waitingForFirstMoveAfterLand = false;
            }
        }

        wasGroundedLastFrame = groundedNow;
    }

    // 由叶子触发器调用
    public void SetNearbyLeaf(ParachuteLeafPickup leaf) => nearbyLeaf = leaf;
    public ParachuteLeafPickup GetNearbyLeaf() => nearbyLeaf;

    public bool HasLeaf() => carriedLeaf != null;
    // 兼容旧用法
    public bool hasLeaf() => HasLeaf();
    public void MarkUsedParachute() => DropLeaf();

    // —— 吸附（保持原“世界尺寸”，并强制指定姿态）——
    void AttachLeaf(Transform leaf)
    {
        if (!carryPoint || !leaf) return;

        // 记录原世界缩放（为了保持尺寸）
        Vector3 worldScale = leaf.lossyScale;

        // 禁物理/碰撞
        carriedLeafRb = leaf.GetComponent<Rigidbody>();
        if (carriedLeafRb)
        {
            carriedLeafRb.isKinematic = true;
            carriedLeafRb.useGravity = false;
            carriedLeafRb.linearVelocity  = Vector3.zero;
            carriedLeafRb.angularVelocity = Vector3.zero;
        }
        carriedLeafCols = leaf.GetComponentsInChildren<Collider>(includeInactive: true);
        foreach (var c in carriedLeafCols) c.enabled = false;

        // 设为子物体（不保持世界姿态，便于直接设置本地位姿）
        leaf.SetParent(carryPoint, worldPositionStays: false);

        // 还原世界尺度：localScale = worldScale / parent.lossyScale
        Vector3 pLossy = carryPoint.lossyScale;
        leaf.localScale = new Vector3(
            worldScale.x / (Mathf.Approximately(pLossy.x, 0f) ? 1f : pLossy.x),
            worldScale.y / (Mathf.Approximately(pLossy.y, 0f) ? 1f : pLossy.y),
            worldScale.z / (Mathf.Approximately(pLossy.z, 0f) ? 1f : pLossy.z)
        );

        // 指定“背上”的本地位置与角度
        leaf.localPosition = attachLocalOffset;
        leaf.localRotation = Quaternion.Euler(attachLocalEuler);

        carriedLeaf = leaf;
    }

    // —— 每帧钉住姿态，防止被别的脚本/物理改掉 —— 
    void LateUpdate()
    {
        if (carriedLeaf && carryPoint)
        {
            carriedLeaf.localPosition = attachLocalOffset;
            carriedLeaf.localRotation = Quaternion.Euler(attachLocalEuler);
        }
    }

    // —— 丢弃到 dropPoint，并在当前姿态基础上 X 再 +90°，且 Y 上抬 dropYOffset —— 
    public void DropLeaf()
    {
        if (!carriedLeaf) return;

        Transform leaf = carriedLeaf;
        carriedLeaf = null;

        // 解除父子关系（保持当前世界姿态）
        leaf.SetParent(null, true);

        // 放到 dropPoint（若为空则保持当前位置）
        if (dropPoint)
        {
            leaf.position = dropPoint.position;
            leaf.rotation = dropPoint.rotation;
        }

        // 在当前姿态基础上，沿自身 X 轴 +90°
        leaf.Rotate(90f, 0f, 0f, Space.Self);

        // 再把世界坐标 Y 抬高一点（避免与地面/玩家穿插）
        if (!Mathf.Approximately(dropYOffset, 0f))
        {
            leaf.position += Vector3.left * dropYOffset;
        }

        // 还原物理/碰撞
        if (carriedLeafRb)
        {
            carriedLeafRb.isKinematic = false;
            carriedLeafRb.useGravity = true;
            carriedLeafRb.linearVelocity = Vector3.zero;
        }
        if (carriedLeafCols != null)
            foreach (var c in carriedLeafCols) c.enabled = true;

        carriedLeafRb = null;
        carriedLeafCols = null;
    }
}
