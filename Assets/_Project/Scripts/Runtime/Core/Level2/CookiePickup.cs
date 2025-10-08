using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CookiePickup : MonoBehaviour
{
    public int amount = 1;

    [Header("SFX（可选）")]
    public bool playSfx = true;              // 是否播放拾取音
    public bool sfxAs2D = true;              // 2D=不受空间影响；3D=在世界位置播
    [Range(0f,1f)] public float sfxVolume = 1f;
    [Tooltip("禁用物体的延迟（秒）。为0也OK；给到0.02~0.05可让3D one-shot更稳。")]
    public float deactivateDelay = 0f;

    bool _consumed = false;                  // 防多次触发

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;                // 作为拾取触发器
    }

    void OnTriggerEnter(Collider other)
    {
        if (_consumed) return;
        if (!other.CompareTag("Player")) return;

        _consumed = true; // 锁一次

        // ① 先加库存 & 刷 UI
        CookiesInventory.Instance?.Add(amount);
        if (CookiesInventory.Instance != null)
            SkillChargeUI.Instance?.Refresh(CookiesInventory.Instance.cookies);

        // ② 播放音效（不依赖本对象的激活状态）
        if (playSfx)
        {
            // 用全局一次性SFX，不会因为本物体被SetActive(false)而被掐断
            GlobalSfx.PlayCookieSfx(transform.position, sfxVolume, sfxAs2D);
        }

        // ③ 最后禁用物体（可选延迟一丢丢，给3D OneShot更充裕的建源时间）
        if (deactivateDelay <= 0f)
        {
            gameObject.SetActive(false);
        }
        else
        {
            StartCoroutine(DeactivateLater());
        }
    }

    System.Collections.IEnumerator DeactivateLater()
    {
        yield return new WaitForSeconds(deactivateDelay);
        gameObject.SetActive(false);
    }
}
