using UnityEngine;

public class rollingCucumber : MonoBehaviour
{
    public float speed = 2f;
    public float leftDistance = 7f;  // 向左滚动的距离
    public float rightDistance = 3f; // 向右滚动的距离
    
    private Vector3 startPos;
    private Vector3 leftEndPos;
    private Vector3 rightEndPos;
    private bool movingToLeft = true;
    private int rotationDirection = 1;

    void Start()
    {
        startPos = transform.position;
        leftEndPos = startPos - transform.right * leftDistance;  // 左边终点
        rightEndPos = startPos + transform.right * rightDistance; // 右边终点
    }

    void Update()
    {
        // 计算目标位置
        Vector3 targetPos = movingToLeft ? leftEndPos : rightEndPos;
        
        // 移动物体
        transform.position = Vector3.MoveTowards(
            transform.position, 
            targetPos, 
            speed * Time.deltaTime
        );

        // 让黄瓜滚动（绕自身Z轴旋转）
        transform.Rotate(0, 0, rotationDirection * speed * 50 * Time.deltaTime);

        // 检查是否到达目标位置
        if (Vector3.Distance(transform.position, targetPos) < 0.001f)
        {
            movingToLeft = !movingToLeft; // 切换移动方向
            rotationDirection *= -1; // 反转旋转方向
        }
    }
}