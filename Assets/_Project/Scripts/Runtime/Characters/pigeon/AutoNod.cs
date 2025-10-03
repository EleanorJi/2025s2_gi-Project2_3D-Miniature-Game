using UnityEngine;
using System.Collections; // 需要使用协程

public class AutoNod : MonoBehaviour
{
    private Animator animator;
    public float nodInterval = 20.0f; // 点头间隔，可在Inspector中调整

    void Start()
    {
        animator = GetComponent<Animator>();
        // 启动协程，每隔一段时间触发一次点头
        StartCoroutine(AutoNodHead());
    }

    IEnumerator AutoNodHead()
    {
        // 这是一个无限循环
        while (true)
        {
            // 等待指定的间隔时间
            yield return new WaitForSeconds(nodInterval);

            // 只有当前不飞行的时候才点头
            bool isFlying = animator.GetBool("IsFlying");
            if (!isFlying)
            {
                // 触发DoNod参数，播放点头动画
                animator.SetTrigger("DoNod");
            }
            // 如果正在飞行，就跳过这次点头，等待下一个周期
        }
    }
}