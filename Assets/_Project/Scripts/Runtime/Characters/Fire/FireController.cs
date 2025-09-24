using UnityEngine;

public class FireController : MonoBehaviour
{
    [Header("FireDownDistanceSetting")]
    // 在Inspector中设置火焰要下降的距离
    public float descentDistance = 2.0f;
    // 下降的速度
    public float descentSpeed = 1.0f;

    // 火焰的初始位置和目标位置
    private Vector3 startPosition;
    private Vector3 targetPosition;

    // 一个标志位，控制是否开始下降
    private bool shouldDescend = false;

    // 添加公共属性来获取火焰状态
    public bool IsDescending { get; private set; } = false;
    public bool IsFullyDescended { get; private set; } = false;

    void Start()
    {
        // 记录火焰初始位置
        startPosition = transform.position;
        // 计算目标位置：初始位置向下移动一定距离
        targetPosition = startPosition + Vector3.down * descentDistance;
    }

    void Update()
    {
        // 如果应该下降，且还没有到达目标位置
        if (shouldDescend && transform.position != targetPosition)
        {
            // 使用MoveTowards平滑地移动到目标位置
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, descentSpeed * Time.deltaTime);

            // 检查是否已经完全下降
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                IsDescending = false;
                IsFullyDescended = true;
            }
        }
    }

    // 提供一个公共方法，让按钮可以调用它来开始下降
    public void StartDescent()
    {
        shouldDescend = true;
        IsDescending = true;
        IsFullyDescended = false;
        Debug.Log(gameObject.name + " 开始下降。");
    }

    // 提供一个公共方法，让按钮可以调用它来停止并复位（如果需要的话）
    public void ResetPosition()
    {
        shouldDescend = false;
        IsDescending = false;
        IsFullyDescended = false;
        transform.position = startPosition;
        Debug.Log(gameObject.name + " 复位。");
    }
}