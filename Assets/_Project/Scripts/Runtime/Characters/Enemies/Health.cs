using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;

    [System.Serializable] public class HealthChangedEvent : UnityEvent<int,int>{}
    public HealthChangedEvent onHealthChanged = new HealthChangedEvent();
    public UnityEvent onDeath = new UnityEvent();

    void Awake() => currentHealth = maxHealth;

    public void TakeDamage(int amount)
    {
        if (currentHealth <= 0) return;
        currentHealth = Mathf.Max(0, currentHealth - amount);
        onHealthChanged.Invoke(currentHealth, maxHealth);
        if (currentHealth == 0) onDeath.Invoke();
    }

    public float Normalized => maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
}
