using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class Health : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 200;

    // 注意：为了兼容你现有的 UI/脚本，这里保持为 public 字段。
    // 如果以后想改成只读属性，记得同步改所有引用处。
    [Tooltip("Current HP")] 
    public int currentHealth = 0;

    [Header("Events")]
    // 参数：current, max
    public UnityEvent<int, int> OnHealthChanged = new UnityEvent<int, int>();
    public UnityEvent OnDeath = new UnityEvent();

    private bool _dead = false;

    private void Awake()
    {
        // 初始化 current 值（若未设置或越界就回满）
        if (currentHealth <= 0 || currentHealth > maxHealth)
            currentHealth = maxHealth;

        _dead = (currentHealth <= 0);

        // 关键：开局就通知一次，避免 UI 等到第一次受击才更新
        SafeInvokeChanged();
        // Debug.Log($"[Health:{name}] Awake -> {currentHealth}/{maxHealth}");
    }

    /// <summary>
    /// Deal damage to this entity.
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (amount <= 0 || _dead) return;

        int before = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - amount);

        if (currentHealth != before)
            SafeInvokeChanged();

        if (currentHealth <= 0 && !_dead)
        {
            _dead = true;
            // Debug.Log($"[Health:{name}] Died.");
            try { OnDeath?.Invoke(); } catch {}
        }
    }

    /// <summary>
    /// Heal this entity (won't exceed maxHealth).
    /// </summary>
    public void Heal(int amount)
    {
        if (amount <= 0 || _dead) return;

        int before = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);

        if (currentHealth != before)
            SafeInvokeChanged();
    }

    /// <summary>
    /// Fully restore HP and clear death state.
    /// </summary>
    public void ResetToFull()
    {
        _dead = false;
        currentHealth = maxHealth;
        SafeInvokeChanged();
        // Debug.Log($"[Health:{name}] ResetToFull -> {currentHealth}/{maxHealth}");
    }

    /// <summary>
    /// Optional helper to set both max & current, then notify.
    /// </summary>
    public void SetMaxAndCurrent(int newMax, int newCurrent)
    {
        maxHealth = Mathf.Max(1, newMax);
        currentHealth = Mathf.Clamp(newCurrent, 0, maxHealth);
        _dead = (currentHealth <= 0);
        SafeInvokeChanged();
    }

    /// <summary>
    /// Whether this entity is dead.
    /// </summary>
    public bool IsDead => _dead;

    private void SafeInvokeChanged()
    {
        try { OnHealthChanged?.Invoke(currentHealth, maxHealth); } catch {}
    }

    // --- Debug helpers in Inspector ---
    [ContextMenu("Debug/Take 20")]
    private void DebugTake20() => TakeDamage(20);

    [ContextMenu("Debug/Heal 20")]
    private void DebugHeal20() => Heal(20);

    [ContextMenu("Debug/Reset To Full")]
    private void DebugResetFull() => ResetToFull();
}
