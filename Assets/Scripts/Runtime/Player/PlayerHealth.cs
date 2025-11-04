using System.Net;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable] public class FloatEvent : UnityEvent<float> { }

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    [SerializeField] public int _currentHealth;   // show in Inspector for debugging
    public int CurrentHealth => _currentHealth;

    [Header("Respawn")]
    public Transform respawnPoint;

    [Header("Events")]
    public UnityEvent OnDied = new UnityEvent();
    public UnityEvent OnRespawned = new UnityEvent();
    public FloatEvent OnHealthPctChanged = new FloatEvent();

    private bool _dead;

    private void Awake()
    {
        // Initialize current health (keep inspector value if >0, else full)
        _currentHealth = (_currentHealth > 0) ? Mathf.Min(_currentHealth, maxHealth) : maxHealth;
        RaisePct();
        Debug.Log($"[PlayerHealth] Awake. max={maxHealth}, cur={_currentHealth}");
    }

    private void Update()
    {
        // Safety net: if someone set CurrentHealth to 0 outside TakeDamage(), ensure death still fires.
        if (!_dead && _currentHealth <= 0)
        {
            Debug.LogWarning("[PlayerHealth] Update safety: health <= 0 detected, invoking Die()");
            DieInternal();
        }
    }

    public void TakeDamage(string source, int amount)
    {
        if (_dead || amount <= 0) return;

        int before = _currentHealth;
        _currentHealth = Mathf.Max(0, _currentHealth - amount);
        RaisePct();
        Debug.Log($"[PlayerHealth] Damage {amount} from {source}. {before} -> {_currentHealth}");

        if (_currentHealth == 0 && !_dead)
        {
            DieInternal();
        }
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        int before = _currentHealth;
        _currentHealth = Mathf.Min(maxHealth, _currentHealth + amount);
        RaisePct();
        Debug.Log($"[PlayerHealth] Heal {amount}. {before} -> {_currentHealth}");
    }

    public void Respawn()
    {
        _currentHealth = maxHealth;
        _dead = false;
        RaisePct();

        // Carry out the complete regeneration logic
        PerformFullRespawn();

        Debug.Log("[PlayerHealth] Respawned.");
        OnRespawned.Invoke();
    }
    
    /// <summary>
    /// Carry out the complete regeneration logic, including the reset of physical state and position.
    /// </summary>
    private void PerformFullRespawn()
    {
        PlayerController playerController = GetComponent<PlayerController>();
        if (playerController != null)
        {
            // Invoke the respawn logic of the PlayerController
            playerController.PerformDirectRespawn();
        }
        else
        {
            // If there is no PlayerController, at least reset the position.
            if (respawnPoint) 
            {
                transform.position = respawnPoint.position;
            }
            else
            {
                // Try to use the CheckpointManager to obtain the respawn point
                Vector3 respawnPosition = CheckpointManager.Instance?.GetLastRespawnPosition() ?? transform.position;
                transform.position = respawnPosition;
            }
        }
    }

    private void RaisePct()
    {
        float pct = maxHealth > 0 ? (float)_currentHealth / maxHealth : 0f;
        OnHealthPctChanged.Invoke(pct);
    }

    private void DieInternal()
    {
        _dead = true;
        Debug.Log("[PlayerHealth] Died -> invoking OnDied");
        OnDied.Invoke();
    }
}
