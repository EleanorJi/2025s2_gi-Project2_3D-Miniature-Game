using UnityEngine;

public class BugCookieHandler : MonoBehaviour
{
    public GameObject cookie; // Cookie to detach from parent and drop when bug is destroyed

    void OnDestroy()
    {
        if (cookie != null)
        {
            // Unparent, make it independent when bug is destroyed
            cookie.transform.parent = null;

            // Ensure Rigidbody exists and enable physics
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
