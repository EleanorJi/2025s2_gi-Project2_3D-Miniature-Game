using UnityEngine;
using System.Collections;

public class CameraFollow : MonoBehaviour
{
    [Header("目标与偏移")]
    public Transform target;                // 玩家角色
    public Vector3 offset = new Vector3(0f, 2f, -5f);

    [Header("跟随设置")]
    public float smoothSpeed = 5f;
    public float mouseSensitivity = 3f;
    public float minPitch = -35f;
    public float maxPitch = 60f;

    [Header("开场动画路径点")]
    public Transform startPoint;           // 起点
    public Transform cookiePoint;          // 饼干位置
    public Transform secondPoint;          // 第二个点
    public Transform thirdPoint;           // 第三个点
    public Transform returnPoint;          // 回到玩家的点（玩家顶上偏前）

    [Header("各阶段时间设置")]
    public float startToCookieRotateTime = 1f;    // 起点转向饼干时间
    public float startToCookieMoveTime = 2f;      // 起点移动到饼干时间
    public float cookieStayTime = 1f;             // 饼干处停留时间
    public float cookieToSecondTime = 2f;         // 饼干退回第二个点时间
    public float secondToThirdTime = 2f;          // 第二个点到第三个点移动时间
    public float thirdPointRotateTime = 1f;       // 第三个点转向玩家时间
    public float thirdToReturnTime = 2f;          // 第三个点到返回点时间
    public float returnToPlayerTime = 1f;         // 返回点回到玩家时间
    public float downwardAngle = 45f;             // 俯角角度

    private float yaw = 0f;
    private float pitch = 10f;
    private bool isIntroPlaying = false;

    void Start()
    {
        if (target != null)
        {
            transform.position = target.position + offset;
            transform.LookAt(target);
            yaw = target.eulerAngles.y;
        }

        // 检查必要的路径点
        if (startPoint != null && cookiePoint != null && secondPoint != null && thirdPoint != null && returnPoint != null)
        {
            StartCoroutine(PlayIntroAnimation());
        }
        else
        {
            Debug.LogWarning("缺少必要的路径点，请检查所有路径点是否已设置");
        }
    }

    IEnumerator PlayIntroAnimation()
    {
        isIntroPlaying = true;

        // 阶段1：起点 → 饼干（先转向再移动）
        transform.position = startPoint.position;
        
        // 1.1 起点转向饼干
        yield return StartCoroutine(RotateToPoint(startPoint.position, cookiePoint.position, startToCookieRotateTime));
        
        // 1.2 移动到饼干位置
        yield return StartCoroutine(MoveToPoint(startPoint.position, cookiePoint.position, startToCookieMoveTime, false));
        
        // 1.3 在饼干处停留
        yield return new WaitForSeconds(cookieStayTime);

        // 阶段2：饼干 → 第二个点（退回并转向第三个点+45度）
        yield return StartCoroutine(MoveAndRotateToPoint(
            cookiePoint.position, 
            secondPoint.position, 
            cookieToSecondTime, 
            true  // 应用45度俯角
        ));

        // 阶段3：第二个点 → 第三个点（仅移动，保持45度俯角）
        yield return StartCoroutine(MoveToPoint(
            secondPoint.position, 
            thirdPoint.position, 
            secondToThirdTime, 
            true  // 保持45度俯角
        ));

        // 阶段4：在第三个点转向玩家（保持45度俯角）
        yield return StartCoroutine(RotateToPointWithAngle(
            thirdPoint.position, 
            target.position, 
            thirdPointRotateTime, 
            downwardAngle
        ));

        // 阶段5：第三个点 → 返回点（水平移动，保持45度俯角）
        yield return StartCoroutine(MoveToPoint(
            thirdPoint.position, 
            returnPoint.position, 
            thirdToReturnTime, 
            true  // 保持45度俯角
        ));

        // 阶段6：返回点 → 玩家位置（平滑过渡到正常视角）
        yield return StartCoroutine(ReturnToPlayer(
            returnPoint.position,
            returnToPlayerTime
        ));

        isIntroPlaying = false;
        
        // 最终过渡到玩家跟随视角
        yield return StartCoroutine(SmoothTransitionToPlayer());
    }

    // 从返回点平滑回到玩家
    IEnumerator ReturnToPlayer(Vector3 fromPosition, float moveTime)
    {
        float timer = 0f;
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;

        while (timer < moveTime)
        {
            timer += Time.deltaTime;
            float t = timer / moveTime;
            t = Mathf.SmoothStep(0f, 1f, t);

            // 计算目标位置（玩家位置+偏移）
            Quaternion targetRotation = Quaternion.Euler(pitch, yaw, 0);
            Vector3 targetPosition = target.position + targetRotation * offset;

            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            
            // 平滑过渡到正常视角
            Vector3 lookDirection = target.position - transform.position;
            if (lookDirection != Vector3.zero)
            {
                Quaternion targetLookRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(startRotation, targetLookRotation, t);
            }

            yield return null;
        }
    }

    // 旋转到目标点（无俯角）
    IEnumerator RotateToPoint(Vector3 fromPosition, Vector3 lookAtTarget, float rotateTime)
    {
        float timer = 0f;
        Quaternion startRotation = transform.rotation;
        Vector3 direction = lookAtTarget - fromPosition;
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        while (timer < rotateTime)
        {
            timer += Time.deltaTime;
            float t = timer / rotateTime;
            transform.position = fromPosition;
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }
        transform.rotation = targetRotation;
    }

    // 旋转到目标点（带俯角）
    IEnumerator RotateToPointWithAngle(Vector3 fromPosition, Vector3 lookAtTarget, float rotateTime, float angle)
    {
        float timer = 0f;
        Quaternion startRotation = transform.rotation;
        Vector3 direction = lookAtTarget - fromPosition;
        direction.y = 0; // 保持水平
        Quaternion targetRotation = Quaternion.LookRotation(direction) * Quaternion.Euler(angle, 0, 0);

        while (timer < rotateTime)
        {
            timer += Time.deltaTime;
            float t = timer / rotateTime;
            transform.position = fromPosition;
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }
        transform.rotation = targetRotation;
    }

    // 移动到目标点（可选择是否保持俯角）
    IEnumerator MoveToPoint(Vector3 fromPosition, Vector3 toPosition, float moveTime, bool keepDownwardAngle)
    {
        float timer = 0f;
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;

        while (timer < moveTime)
        {
            timer += Time.deltaTime;
            float t = timer / moveTime;
            t = Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector3.Lerp(startPosition, toPosition, t);
            
            if (keepDownwardAngle)
            {
                Vector3 moveDirection = (toPosition - fromPosition);
                moveDirection.y = 0;
                if (moveDirection != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection.normalized) * Quaternion.Euler(downwardAngle, 0, 0);
                    transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
                }
            }
            else
            {
                transform.rotation = startRotation;
            }
            yield return null;
        }
        transform.position = toPosition;
    }

    // 移动并旋转到目标点（带俯角）
    IEnumerator MoveAndRotateToPoint(Vector3 fromPosition, Vector3 toPosition, float moveTime, bool useDownwardAngle)
    {
        float timer = 0f;
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        
        // 计算目标旋转：面向第三个点 + 俯角
        Vector3 targetDirection = thirdPoint.position - toPosition;
        targetDirection.y = 0;
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection.normalized) * Quaternion.Euler(downwardAngle, 0, 0);

        while (timer < moveTime)
        {
            timer += Time.deltaTime;
            float t = timer / moveTime;
            t = Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector3.Lerp(startPosition, toPosition, t);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }
        transform.position = toPosition;
        transform.rotation = targetRotation;
    }

    IEnumerator SmoothTransitionToPlayer()
    {
        float transitionDuration = 1f;
        float timer = 0f;
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;

        while (timer < transitionDuration)
        {
            timer += Time.deltaTime;
            float t = timer / transitionDuration;
            t = Mathf.SmoothStep(0f, 1f, t);

            Quaternion targetRot = Quaternion.Euler(pitch, yaw, 0);
            Vector3 targetPos = target.position + targetRot * offset;

            transform.position = Vector3.Lerp(startPosition, targetPos, t);
            
            Vector3 lookDir = target.position - transform.position;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(startRotation, Quaternion.LookRotation(lookDir), t);
            }
            yield return null;
        }
    }

    void LateUpdate()
    {
        if (target == null || isIntroPlaying) return;

        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 desiredPosition = target.position + rotation * offset;

        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.LookAt(target);
    }

    public void SkipIntroAnimation()
    {
        if (isIntroPlaying)
        {
            StopAllCoroutines();
            isIntroPlaying = false;
            StartCoroutine(SmoothTransitionToPlayer());
        }
    }

    void OnDrawGizmosSelected()
    {
        // 绘制路径点
        DrawPointWithLabel(startPoint, "起点", Color.green);
        DrawPointWithLabel(cookiePoint, "饼干位置", Color.red);
        DrawPointWithLabel(secondPoint, "第二个点", Color.blue);
        DrawPointWithLabel(thirdPoint, "第三个点", Color.yellow);
        DrawPointWithLabel(returnPoint, "返回点", Color.cyan);

        // 绘制路径线
        if (startPoint && cookiePoint)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(startPoint.position, cookiePoint.position);
            DrawArrow(startPoint.position, (cookiePoint.position - startPoint.position).normalized, 1f);
        }
        if (cookiePoint && secondPoint)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(cookiePoint.position, secondPoint.position);
            DrawArrow(cookiePoint.position, (secondPoint.position - cookiePoint.position).normalized, 1f);
        }
        if (secondPoint && thirdPoint)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(secondPoint.position, thirdPoint.position);
            DrawArrow(secondPoint.position, (thirdPoint.position - secondPoint.position).normalized, 1f);
        }
        if (thirdPoint && returnPoint)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(thirdPoint.position, returnPoint.position);
            DrawArrow(thirdPoint.position, (returnPoint.position - thirdPoint.position).normalized, 1f);
        }
        if (returnPoint && target)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(returnPoint.position, target.position);
            DrawArrow(returnPoint.position, (target.position - returnPoint.position).normalized, 1f);
        }
    }

    void DrawPointWithLabel(Transform point, string label, Color color)
    {
        if (point != null)
        {
            Gizmos.color = color;
            Gizmos.DrawSphere(point.position, 0.3f);
            #if UNITY_EDITOR
            GUIStyle style = new GUIStyle();
            style.normal.textColor = color;
            style.fontSize = 12;
            UnityEditor.Handles.Label(point.position + Vector3.up * 0.5f, label, style);
            #endif
        }
    }

    void DrawArrow(Vector3 position, Vector3 direction, float size)
    {
        Gizmos.DrawRay(position, direction * size);
        Gizmos.DrawRay(position + direction * size, Quaternion.Euler(0, 45, 0) * -direction * size * 0.3f);
        Gizmos.DrawRay(position + direction * size, Quaternion.Euler(0, -45, 0) * -direction * size * 0.3f);
    }
}