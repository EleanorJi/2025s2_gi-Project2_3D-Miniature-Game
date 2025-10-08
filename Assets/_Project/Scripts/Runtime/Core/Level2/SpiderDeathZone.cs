using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SpiderDeathZone : MonoBehaviour
{
    [Header("过滤")]
    public string playerTag = "Player";

    [Header("这一次想显示的内容（可留空=用模板默认）")]
    [TextArea]
    public string overrideMessage;     // 例如：“你被蜘蛛网困住了…”
    public Sprite overrideSprite;      // 这次的专用图片（可不填）

    [Header("这一次的显示时长（留空/<=0 用模板默认）")]
    public float holdSeconds = 4f;

    bool _busy;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_busy) return;
        if (!other.CompareTag(playerTag)) return;

        var root = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform;
        var pc = root.GetComponent<PlayerController>();
        if (!pc) return;

        _busy = true;

        // 1) 先真正死亡（会回到你的存档点/重生点）
        pc.Die();
        GlobalSfx.PlayDeathSfx();


        // 2) 让全局模板显示（本次允许覆盖文案/图片/时长）
        if (DeathUIOverlay.Instance)
        {
            if (holdSeconds > 0f)
                DeathUIOverlay.Instance.Show(overrideMessage, overrideSprite, holdSeconds);
            else
                DeathUIOverlay.Instance.Show(overrideMessage, overrideSprite, null); // 用默认时长
        }

        // 确保不会一帧内多次触发
        StartCoroutine(ClearBusyNextFrame());
    }

    System.Collections.IEnumerator ClearBusyNextFrame()
    {
        yield return null;
        _busy = false;
    }
}
