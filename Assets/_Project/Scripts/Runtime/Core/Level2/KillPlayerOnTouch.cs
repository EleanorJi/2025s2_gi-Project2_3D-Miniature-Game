using UnityEngine;

public class KillPlayerOnTouch : MonoBehaviour
{
    [Header("Death UI（可在组件里自定义覆盖）")]
    [TextArea] public string deathMessage = "你被虫子干掉了…";
    public Sprite deathSprite;
    public float deathDuration = 4f; // <=0 则用模板默认时长

    // 防止一次接触同时触发 OnCollision 和 OnTrigger 导致重复播 UI
    bool _busy;

    void TriggerDeath(GameObject go)
    {
        if (_busy || go == null) return;

        var pc = go.GetComponent<PlayerController>();
        if (!pc) return;

        _busy = true;

        // 先按你现有流程死亡（回到重生点等）
        pc.Die();
        GlobalSfx.PlayDeathSfx();


        // 然后调用全局死亡模板（文字/图片/时长可在本组件里改；留空用模板默认）
        DeathUIOverlay.Instance?.Show(
            string.IsNullOrEmpty(deathMessage) ? null : deathMessage,
            deathSprite,
            (deathDuration > 0f) ? deathDuration : (float?)null
        );

        // 小冷却，避免同一帧或短时间内重复触发
        Invoke(nameof(ResetBusy), 0.2f);
    }

    void ResetBusy() => _busy = false;

    private void OnCollisionEnter(Collision c)
    {
        if (c.collider.CompareTag("Player"))
            TriggerDeath(c.collider.gameObject);
    }

    // 保险：若某个碰撞体被设为 Trigger 也能触发
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            TriggerDeath(other.gameObject);
    }
}
