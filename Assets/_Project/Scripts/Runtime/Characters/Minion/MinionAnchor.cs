using UnityEngine;

public class MinionAnchor : MonoBehaviour
{
    [Header("Follow Target")]
    public Transform follow;                // 玩家
    public Vector3 localOffset;             // 围绕玩家的偏移（由召唤时设置）
    public float stickSpeed = 20f;          // 越大越紧跟

    [Header("Ground Snap")]
    public bool snapToGround = true;
    public float rayHeight = 3f;            // 从上往下起点高度
    public float extraGroundOffset = 0.02f; // 轻微抬高避免卡地
    public LayerMask groundMask = ~0;       // 只勾 Ground 更好

    Collider col;
    float footExtentY = 0.1f;

    void Awake()
    {
        col = GetComponent<Collider>();
        UpdateFootExtent();
    }

    void OnValidate()
    {
        // 在编辑器参数变化时也更新一下
        if (!Application.isPlaying)
        {
            col = GetComponent<Collider>();
            UpdateFootExtent();
        }
    }

    void UpdateFootExtent()
    {
        if (col != null)
        {
            // 世界对齐包围盒的一半高度，适配任何朝向/缩放
            footExtentY = Mathf.Max(col.bounds.extents.y, 0.05f);
        }
    }

    void LateUpdate()
    {
        if (!follow) return;

        // 水平跟随
        Vector3 basePos = follow.position + localOffset;

        float desiredY = basePos.y;

        if (snapToGround)
        {
            // 从上方发射射线
            Vector3 rayStart = basePos + Vector3.up * rayHeight;
            float castDist = rayHeight + 5f;

            bool hit = Physics.Raycast(rayStart, Vector3.down,
                                       out RaycastHit hitInfo, castDist,
                                       groundMask, QueryTriggerInteraction.Ignore);

            // 兜底：用 SphereCast 更宽容（薄地面/小台阶）
            if (!hit)
            {
                hit = Physics.SphereCast(rayStart, 0.15f, Vector3.down,
                                         out hitInfo, castDist,
                                         groundMask, QueryTriggerInteraction.Ignore);
            }

            if (hit)
            {
                desiredY = hitInfo.point.y + footExtentY + extraGroundOffset;
            }
        }

        Vector3 desired = new Vector3(basePos.x, desiredY, basePos.z);

        // 平滑插值，避免跳动
        float t = 1f - Mathf.Exp(-stickSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desired, t);
    }
}
