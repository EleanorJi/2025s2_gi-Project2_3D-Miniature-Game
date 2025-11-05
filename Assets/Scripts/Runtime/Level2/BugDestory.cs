using UnityEngine;

public class BugDestory : MonoBehaviour
{
    // Prefab to drop when destroyed (e.g., cookie, parachute, etc.)
    public GameObject dropPrefab;

    // Prevent duplicate drops, ensure each bug only drops once
    private bool isDying = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
        // Collision with PlayerPoisonShooter's poison particles
        else if (other.CompareTag("PoisonParticle"))
        {
          
            
            Die();
        }
    }

    // Determine if the bug should drop an item
    bool ShouldDropOnDeath()
    {
        // Find child object named "Cookie"
        Transform cookie = transform.Find("Cookie");
        if (cookie != null)
        {
            // Only drop item when Cookie child is in active state
            return cookie.gameObject.activeInHierarchy;
        }

        // If no child named Cookie, default to dropping item
        return true;
    }

    // Unified death handler: destroy bug and generate dropped item if needed
    public void Die()
    {
        if (isDying) return;
        isDying = true;

        // Check if should drop item
        if (ShouldDropOnDeath() && dropPrefab != null)
        {
            Instantiate(dropPrefab, transform.position, Quaternion.identity);
        }

        // Destroy bug
        Destroy(gameObject);
    }
}
