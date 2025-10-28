using UnityEngine;
using System.Collections;

public class AutoNod : MonoBehaviour
{
    private Animator animator;
    public float nodInterval = 20.0f; // The nod interval can be adjusted in the Inspector.

    void Start()
    {
        animator = GetComponent<Animator>();
        // Start the coroutine and trigger a nod every certain period of time.
        StartCoroutine(AutoNodHead());
    }

    IEnumerator AutoNodHead()
    {
        // This is an infinite loop.
        while (true)
        {
            // Wait for the specified interval of time
            yield return new WaitForSeconds(nodInterval);

            // Nod only when it is not "end" at the moment.
            bool isEnd = animator.GetBool("IsEnd");
            if (!isEnd)
            {
                // Trigger the DoNod parameter and play the nod animation
                animator.SetTrigger("DoNod");
            }
        }
    }
}