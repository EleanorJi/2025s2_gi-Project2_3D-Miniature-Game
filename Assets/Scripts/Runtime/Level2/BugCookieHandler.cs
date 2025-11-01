using UnityEngine;

public class BugCookieHandler : MonoBehaviour
{
    public GameObject cookie; // 需要在虫子被销毁时将该饼干从父对象脱离并落下

    void OnDestroy()
    {
        if (cookie != null)
        {
            // 脱离父物体，使其在场景中独立
            cookie.transform.parent = null;

            // 确保有 Rigidbody，启用重力让其落下
            Rigidbody rb = cookie.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = cookie.AddComponent<Rigidbody>();
            }
            rb.isKinematic = false;
            rb.useGravity = true;

           
            var follower = cookie.GetComponent<CookieFollower>();
            if (follower != null)
            {
                follower.enabled = false;
            }
        }
    }
}
