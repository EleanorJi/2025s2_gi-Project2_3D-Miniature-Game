using UnityEngine;

public class KillPlayerOnTouch : MonoBehaviour
{
    private void OnCollisionEnter(Collision c)
    {
        if (c.collider.CompareTag("Player"))
        {
            var pc = c.collider.GetComponent<PlayerController>();
            if (pc) pc.Die();
        }
    }

    // 保险：如果你某只虫子的碰撞体不小心勾成 Trigger，也能触发
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var pc = other.GetComponent<PlayerController>();
            if (pc) pc.Die();
        }
    }
}
