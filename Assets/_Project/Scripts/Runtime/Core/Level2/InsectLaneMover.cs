using UnityEngine;

public class InsectLaneMover : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;
    public float speed = 2f;
    public bool flipChildrenFacing = true;

    Vector3 _target;
    int _dir = 1; // 1->B, -1->A

    void Start()
    {
        if (pointA && pointB) _target = pointB.position;
    }

    void Update()
    {
        if (!pointA || !pointB) return;

        transform.position = Vector3.MoveTowards(transform.position, _target, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, _target) < 0.01f)
        {
        
            _dir *= -1;
            _target = (_dir > 0) ? pointB.position : pointA.position;

            if (flipChildrenFacing)
            {
                foreach (Transform child in transform)
                {
                    // only flip horizontal facing (180° around Y axis)
                    child.Rotate(0f, 0f, 180f);
                }
            }
        }
    }
}
