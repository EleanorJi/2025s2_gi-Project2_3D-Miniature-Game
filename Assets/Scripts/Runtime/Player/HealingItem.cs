using UnityEngine;

public class HealingItem : MonoBehaviour
{

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<PlayerHealth>().Heal(30);
            Debug.Log("a" + other.GetComponent<PlayerHealth>()._currentHealth);

            Destroy(gameObject);
        }
    }
}
