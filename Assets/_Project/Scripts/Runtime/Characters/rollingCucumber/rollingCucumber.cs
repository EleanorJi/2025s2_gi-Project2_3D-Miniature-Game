using UnityEngine;

public class rollingCucumber : MonoBehaviour
{
    public float speed = 2f;
    public float leftDistance = 7f;  // The distance scrolled to the left
    public float rightDistance = 3f; // The distance scrolled to the right
    
    private Vector3 startPos;
    private Vector3 leftEndPos;
    private Vector3 rightEndPos;
    private bool movingToLeft = true;
    private int rotationDirection = 1;

    // 添加公共属性供其他脚本访问
    public bool IsMovingLeft => movingToLeft;
    public Vector3 LeftEndPos => leftEndPos;
    public Vector3 RightEndPos => rightEndPos;
    
    void Start()
    {
        startPos = transform.position;
        leftEndPos = startPos - transform.right * leftDistance;  // Left end point
        rightEndPos = startPos + transform.right * rightDistance; // right end point
    }

    void Update()
    {
        // Calculate the target position
        Vector3 targetPos = movingToLeft ? leftEndPos : rightEndPos;
        
        // Moving object
        transform.position = Vector3.MoveTowards(
            transform.position, 
            targetPos, 
            speed * Time.deltaTime
        );

        // Make the cucumber roll (rotate around its own Z-axis)
        transform.Rotate(0, 0, rotationDirection * speed * 50 * Time.deltaTime);

        // Check if the target position has been reached
        if (Vector3.Distance(transform.position, targetPos) < 0.001f)
        {
            movingToLeft = !movingToLeft;
            rotationDirection *= -1;
        }
    }
}