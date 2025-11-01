using UnityEngine;

public class CookieFollower : MonoBehaviour
{
    public Transform target;      // 需要跟随的目标（虫子）
    public Vector3 offset = new Vector3(0f, 0.15f, 0f);

    private bool hasDropped = false;

    public GameObject cookie;

    void Update()
    {
        if (target != null)
        {
            // 跟随目标位置与旋转
            transform.position = target.position + offset;
            transform.rotation = target.rotation;
        }
        //else
        //{
        //    // 目标不存在，脱离父物体并落下
        //    if (!hasDropped)
        //    {
        //        DropAndFall();
        //    }
        //}
    }

    //void DropAndFall()
    //{
    //    hasDropped = true;
    //    // 取消父子关系（如果仍然是父子关系）
    //    if (transform.parent != null)
    //        transform.parent = null;

    //    // 确保有 Rigidbody 组件，启用重力落下
    //    Rigidbody rb = GetComponent<Rigidbody>();
    //    if (rb == null)
    //    {
    //        rb = gameObject.AddComponent<Rigidbody>();
    //    }
    //    rb.isKinematic = false;
    //    rb.useGravity = true;

    //    // 不再跟随
    //    this.enabled = false;
    //}

  
}
