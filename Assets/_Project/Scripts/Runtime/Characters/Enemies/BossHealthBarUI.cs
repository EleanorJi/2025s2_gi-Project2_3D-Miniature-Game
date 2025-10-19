using UnityEngine;
using UnityEngine.UI;

public class BossHealthBarUI : MonoBehaviour
{
    public Health target;   // Boss Health
    public Slider slider;

    private void Awake()
    {
        if (!slider) slider = GetComponentInChildren<Slider>(true);
        if (!target)
        {
            var b = GameObject.FindGameObjectWithTag("Boss");
            if (b) target = b.GetComponent<Health>();
        }

        if (slider) { slider.minValue = 0f; slider.maxValue = 1f; }

        if (target)
        {
            target.OnHealthChanged.AddListener(HandleChanged);
            HandleChanged(target.currentHealth, target.maxHealth); // 初始刷新
        }
    }

    private void OnDisable()
    {
        if (target) target.OnHealthChanged.RemoveListener(HandleChanged);
    }

    private void HandleChanged(int cur, int max)
    {
        if (!slider) return;
        slider.value = (max > 0) ? (float)cur / max : 0f;
    }

    // ---- 新增：外部可强制刷新一次（用于重生后立刻更新 UI） ----
    public void RefreshNow()
    {
        if (target) HandleChanged(target.currentHealth, target.maxHealth);
    }
}
