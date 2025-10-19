using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBarUI : MonoBehaviour
{
    public PlayerHealth target;   // 留空会自动找 Tag=Player
    public Slider slider;         // 留空会在自己子物体中找

    private void Awake()
    {
        if (!slider) slider = GetComponentInChildren<Slider>(true);

        if (!target)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) target = p.GetComponent<PlayerHealth>();
            if (!target) target = FindObjectOfType<PlayerHealth>();
        }

        if (slider)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
        }

        if (target)
        {
            target.OnHealthPctChanged.AddListener(HandlePct);
            HandlePct(target.maxHealth > 0 ? (float)target.CurrentHealth / target.maxHealth : 0f);
        }
        else
        {
            Debug.LogWarning("[PlayerHealthBarUI] No PlayerHealth found.");
        }
    }

    private void OnDisable()
    {
        if (target) target.OnHealthPctChanged.RemoveListener(HandlePct);
    }

    private void HandlePct(float pct)
    {
        if (slider) slider.value = pct;
    }
}
