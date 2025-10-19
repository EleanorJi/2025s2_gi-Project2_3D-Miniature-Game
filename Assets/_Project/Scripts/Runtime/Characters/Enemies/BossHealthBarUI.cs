using UnityEngine;
using UnityEngine.UI;

public class BossHealthBarUI : MonoBehaviour
{
    public Health target;   // Boss Health
    public Slider slider;
    
    [Header("World Space Positioning")]
    [Tooltip("Boss transform to follow (will auto-find by tag 'Boss' if left empty)")]
    public Transform bossTransform;
    
    [Tooltip("Offset above the boss (in world units)")]
    public Vector3 worldOffset = new Vector3(0, 3f, 0);
    
    [Tooltip("Camera reference (will auto-find Main Camera if left empty)")]
    public Camera uiCamera;

    private void Awake()
    {
        if (!slider) slider = GetComponentInChildren<Slider>(true);
        
        // Auto-find boss if not assigned
        if (!target)
        {
            var b = GameObject.FindGameObjectWithTag("Boss");
            if (b) target = b.GetComponent<Health>();
        }
        
        // Auto-find boss transform if not assigned
        if (!bossTransform)
        {
            var b = GameObject.FindGameObjectWithTag("Boss");
            if (b) bossTransform = b.transform;
        }
        
        // Auto-find camera if not assigned
        if (!uiCamera)
        {
            uiCamera = Camera.main;
            if (!uiCamera) uiCamera = FindObjectOfType<Camera>();
        }

        if (slider) { slider.minValue = 0f; slider.maxValue = 1f; }

        if (target)
        {
            target.OnHealthChanged.AddListener(HandleChanged);
            HandleChanged(target.currentHealth, target.maxHealth); // 初始刷新
        }
    }

    private void Update()
    {
        // Update position to follow boss
        if (bossTransform && uiCamera)
        {
            Vector3 worldPos = bossTransform.position + worldOffset;
            Vector3 screenPos = uiCamera.WorldToScreenPoint(worldPos);
            
            // Only show if boss is in front of camera
            if (screenPos.z > 0)
            {
                transform.position = screenPos;
                gameObject.SetActive(true);
            }
            else
            {
                gameObject.SetActive(false);
            }
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
