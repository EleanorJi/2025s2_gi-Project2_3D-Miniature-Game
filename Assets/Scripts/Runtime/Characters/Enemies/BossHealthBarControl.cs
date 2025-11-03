using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class BossHealthBarControl : MonoBehaviour
{
    public Image hpImg;               // normal HP Bar
    public Image hpEffectImg;         // delayed effect HP Bar
    public float buffTime = 0.5f;

    public Health externalHealth;


    public float maxHp = 200f;
    public float currentHp = 200f;

    // win panel
    public GameObject winPanel;
    public bool showWinOnDeath = true;
    public GameObject hideOnWin;     
    public string continueSceneName = "StartScene";

    public bool faceCamera = true;
    public Camera cam;

    Coroutine _fxCo;

    void Awake()
    {
        if (!cam) cam = Camera.main;


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

            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }

        // win panel click to continue
        if (winPanel && winPanel.activeSelf && Input.GetMouseButtonDown(0))
        {
            if (!string.IsNullOrEmpty(continueSceneName))
                SceneManager.LoadScene(continueSceneName);
        }
    }


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
