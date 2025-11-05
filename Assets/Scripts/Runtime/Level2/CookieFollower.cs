using UnityEngine;

public class CookieFollower : MonoBehaviour
{
    public Transform target;      // Target to follow (insect)
    public Vector3 offset = new Vector3(0f, 0.15f, 0f);

    private bool hasDropped = false;

    public GameObject cookie;

    void Update()
    {
        if (target != null)
        {
            // Follow target position and rotation
            transform.position = target.position + offset;
            transform.rotation = target.rotation;
        }
    }

}
