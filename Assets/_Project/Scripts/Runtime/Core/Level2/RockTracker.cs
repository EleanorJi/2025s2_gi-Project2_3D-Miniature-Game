using UnityEngine;

public class RockTracker : MonoBehaviour
{
    public RockSurface currentRock      { get; private set; }
    public RockSurface lastJumpFromRock { get; private set; }

    /// 起跳时可显式调用；但走路换石头时也会自动更新
    public void MarkJump() { if (currentRock) lastJumpFromRock = currentRock; }

    void OnCollisionEnter(Collision c)
    {
        var r = c.collider.GetComponentInParent<RockSurface>() ?? c.collider.GetComponent<RockSurface>();
        if (!r && !c.collider.CompareTag("Rock")) return;  // 既没组件也不是Rock标签就忽略
        if (!r) r = c.collider.GetComponentInParent<RockSurface>() ?? c.collider.GetComponent<RockSurface>();

        if (r)
        {
            if (currentRock != null && currentRock != r)
                lastJumpFromRock = currentRock;     // ★切换石头时，旧的成为“上一块”
            currentRock = r;

            if (lastJumpFromRock == null)
                lastJumpFromRock = currentRock;     // ★第一次落地种子，保证首进蜂蜜也能回
        }
    }

    void OnCollisionExit(Collision c)
    {
        var r = c.collider.GetComponentInParent<RockSurface>() ?? c.collider.GetComponent<RockSurface>();
        if (r && r == currentRock) currentRock = null;
        // 注意：Exit 不动 lastJumpFromRock
    }
}
