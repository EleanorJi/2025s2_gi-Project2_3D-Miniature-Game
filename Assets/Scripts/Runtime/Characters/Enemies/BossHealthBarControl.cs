using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class BossHealthBarControl : MonoBehaviour
{
    public Image hpImg;               // 正常条
    public Image hpEffectImg;         // 延迟缓冲条
    public float buffTime = 0.5f;

    // 可选：外部血量源（推荐把 Pigeon 上的 Health 拖到这里）
    public Health externalHealth;

    // 纯数值模式（不连 externalHealth 时使用）
    public float maxHp = 200f;
    public float currentHp = 200f;

    // 胜利面板（你的 GameOverScreen/Win(1)）
    public GameObject winPanel;
    public bool showWinOnDeath = true;
    public GameObject hideOnWin;      // 可选：例如玩家HP条父物体
    public string continueSceneName = "StartScene";

    // 让世界空间血条面向相机（你是 World Space Canvas）
    public bool faceCamera = true;
    public Camera cam;

    Coroutine _fxCo;

    void Awake()
    {
        if (!cam) cam = Camera.main;

        // 如果有 Health，则以 Health 为准，初始化一次
        if (externalHealth)
        {
            maxHp = externalHealth.maxHealth;
            currentHp = externalHealth.currentHealth;
            externalHealth.OnHealthChanged.AddListener(OnHealthChanged);
            externalHealth.OnDeath.AddListener(OnBossDead);
        }

        ApplyToImages();
        if (winPanel) winPanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (externalHealth)
        {
            externalHealth.OnHealthChanged.RemoveListener(OnHealthChanged);
            externalHealth.OnDeath.RemoveListener(OnBossDead);
        }
    }

    void Update()
    {
        if (faceCamera && cam)
        {
            // 让血条朝向相机（世界空间）
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }

        // 胜利界面点击继续
        if (winPanel && winPanel.activeSelf && Input.GetMouseButtonDown(0))
        {
            if (!string.IsNullOrEmpty(continueSceneName))
                SceneManager.LoadScene(continueSceneName);
        }
    }

    // —— 对外便捷接口：被伤害方脚本也可以直接调用这个（在没有 externalHealth 时）——
    public void ApplyDamage(float amount)
    {
        if (externalHealth)
        {
            externalHealth.TakeDamage(Mathf.RoundToInt(amount));
            return;
        }

        currentHp = Mathf.Clamp(currentHp - amount, 0f, maxHp);
        ApplyToImages();

        if (currentHp <= 0f) OnBossDead();
    }

    public void SetHealth(float hp)
    {
        if (externalHealth)
        {
            externalHealth.SetMaxAndCurrent(externalHealth.maxHealth, Mathf.RoundToInt(hp));
            return;
        }

        currentHp = Mathf.Clamp(hp, 0f, maxHp);
        ApplyToImages();
    }

    // —— 监听 Health 源 —— 
    void OnHealthChanged(int cur, int max)
    {
        maxHp = max;
        currentHp = cur;
        ApplyToImages();
    }

    void OnBossDead()
    {
        if (showWinOnDeath && winPanel) winPanel.SetActive(true);
        if (hideOnWin) hideOnWin.SetActive(false);
    }

    // —— 把数值应用到两张 Image（含缓冲过渡）——
    void ApplyToImages()
    {
        if (!hpImg || !hpEffectImg) return;

        float pct = (maxHp > 0f) ? currentHp / maxHp : 0f;
        pct = Mathf.Clamp01(pct);

        hpImg.fillAmount = pct;

        if (_fxCo != null) StopCoroutine(_fxCo);
        _fxCo = StartCoroutine(EffectLerpTo(pct));
    }

    IEnumerator EffectLerpTo(float target)
    {
        float start = hpEffectImg.fillAmount;
        float t = 0f;
        while (t < buffTime)
        {
            t += Time.deltaTime;
            hpEffectImg.fillAmount = Mathf.Lerp(start, target, t / buffTime);
            yield return null;
        }
        hpEffectImg.fillAmount = target;
    }
}
