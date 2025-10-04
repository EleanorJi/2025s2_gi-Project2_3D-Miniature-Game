using UnityEngine;
using System.Collections;

public class CameraFollow : MonoBehaviour
{
    [Header("目标与偏移")]
    public Transform target;                // 玩家角色
    public Vector3 offset = new Vector3(0f, 1.5f, -4f);

    [Header("跟随设置")]
    public float smoothSpeed = 5f;
    public float mouseSensitivity = 3f;
    public float minPitch = -35f;
    public float maxPitch = 60f;

    [Header("开场动画路径点")]
    public Transform startPoint;           
    public Transform cookiePosition;          
    public Transform endPoint;          
    public Transform connerA;          
    public Transform connerB;          
    public Transform returnPoint;

    [Header("各阶段时间设置")]
    public float startToCookieRotateTime = 1f;    // 起点转向饼干时间
    public float startToCookieMoveTime = 3f;      // 起点移动到饼干时间
    public float cookieStayTime = 1f;             // 饼干处停留时间
    public float cookieToSecondTime = 2f;         // 饼干退回第二个点时间
    public float secondToThirdATime = 5f;         // 第二个点到拐弯点A移动时间
    public float thirdCurveTime = 3f;             // 拐弯移动时间
    public float thirdToReturnTime = 5f;          // 第三个点到返回点时间
    public float returnToPlayerTime = 1f;         // 返回点回到玩家时间
    public float downwardAngle = 45f;             // 俯角角度

    private float yaw = 0f;
    private float pitch = 10f;
    private bool isIntroPlaying = false;
    private bool intro = true;

    // 新增：锁定垂直视角用
    bool _lockPitch = false;
    float _lockedPitchValue = 0f;

    void Start()
    {
        if (target != null)
        {
            transform.position = target.position + offset;
            transform.LookAt(target);
            yaw = target.eulerAngles.y;
        }

        if (startPoint != null && cookiePosition != null && endPoint != null && 
            connerA != null && connerB != null && returnPoint != null && intro)
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

        // 阶段1：起点 → 饼干
        transform.position = startPoint.position;

        yield return StartCoroutine(RotateToPoint(cookiePosition.position, startToCookieRotateTime));

        yield return StartCoroutine(MoveToPoint(
            transform.position, transform.rotation, 
            cookiePosition.position, startToCookieMoveTime, false
        ));

        yield return new WaitForSeconds(cookieStayTime);

        // 阶段2：饼干 → 第二个点
        yield return StartCoroutine(MoveAndRotateToPoint(
            transform.position, transform.rotation,
            endPoint.position, cookieToSecondTime, true
        ));

        // 合并阶段3、4、5：第二个点 → 第三个点A → 第三个点B（圆弧）→ 返回点
        yield return StartCoroutine(ContinuousMoveStage3To5(
            endPoint.position, 
            connerA.position,
            connerB.position,
            returnPoint.position,
            secondToThirdATime + thirdCurveTime + thirdToReturnTime  // 总时间
        ));
        // 阶段6：返回点 → 玩家
        yield return StartCoroutine(ReturnToPlayer(returnToPlayerTime));

        isIntroPlaying = false;
        
        // 最终过渡到玩家跟随视角
        yield return StartCoroutine(SmoothTransitionToPlayer());
    }
    IEnumerator ContinuousMoveStage3To5(Vector3 stage3Start, Vector3 stage4Start, Vector3 stage4End, Vector3 stage5End, float totalTime)
    {
        float timer = 0f;
        
        // 预先计算各阶段的开始和结束时间
        float stage3EndTime = secondToThirdATime;
        float stage4EndTime = stage3EndTime + thirdCurveTime;
        float stage5EndTime = totalTime;
        
        // 预先计算各阶段的目标旋转
        Quaternion stage3StartRot = transform.rotation;
        Quaternion stage3TargetRot = Quaternion.LookRotation((stage4Start - stage3Start).normalized) * Quaternion.Euler(downwardAngle, 0, 0);
        Quaternion stage4TargetRot = Quaternion.LookRotation((stage5End - stage4End).normalized) * Quaternion.Euler(downwardAngle, 0, 0);
        Quaternion stage5TargetRot = stage4TargetRot; // 阶段5保持阶段4的旋转方向

        while (timer < totalTime)
        {
            timer += Time.deltaTime;
            
            if (timer <= stage3EndTime)
            {
                // 阶段3：线性移动
                float t = timer / stage3EndTime;
                transform.position = Vector3.Lerp(stage3Start, stage4Start, t);
                transform.rotation = Quaternion.Lerp(stage3StartRot, stage3TargetRot, t);
            }
            else if (timer <= stage4EndTime)
            {
                // 阶段4：贝塞尔曲线移动
                float t = (timer - stage3EndTime) / thirdCurveTime;
                Vector3 controlPoint = CalculateHorizontalControlPoint(stage4Start, stage4End);
                transform.position = CalculateBezierPoint(stage4Start, controlPoint, stage4End, t);
                transform.rotation = Quaternion.Lerp(stage3TargetRot, stage4TargetRot, t);
            }
            else
            {
                // 阶段5：线性移动
                float t = (timer - stage4EndTime) / thirdToReturnTime;
                transform.position = Vector3.Lerp(stage4End, stage5End, t);
                transform.rotation = Quaternion.Lerp(stage4TargetRot, stage5TargetRot, t);
            }

            yield return null;
        }
        
        // 确保最终位置准确
        transform.position = stage5End;
        transform.rotation = stage5TargetRot;
    }

    Vector3 CalculateHorizontalControlPoint(Vector3 start, Vector3 end)
    {
        Vector3 midPoint = (start + end) * 0.5f;
        Vector3 direction = (end - start).normalized;
        Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
        float curveAmount = Vector3.Distance(start, end) * 0.5f;
        return midPoint + perpendicular * curveAmount;
    }

    Vector3 CalculateBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float u = 1 - t;
        return u * u * p0 + 2 * u * t * p1 + t * t * p2;
    }

    IEnumerator ReturnToPlayer(float moveTime)
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

    IEnumerator RotateToPoint(Vector3 lookAtTarget, float rotateTime)
    {
        float timer = 0f;
        Quaternion startRotation = transform.rotation;
        Vector3 direction = lookAtTarget - transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        while (timer < rotateTime)
        {
            timer += Time.deltaTime;
            float t = timer / rotateTime;
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }
    }

    IEnumerator MoveToPoint(Vector3 startPos, Quaternion startRot, Vector3 toPosition, float moveTime, bool keepDownwardAngle)
    {
        float timer = 0f;

        while (timer < moveTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / moveTime);

            transform.position = Vector3.Lerp(startPos, toPosition, t);

            if (keepDownwardAngle)
            {
                Vector3 moveDirection = (toPosition - startPos);
                moveDirection.y = 0;
                if (moveDirection != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection.normalized) * Quaternion.Euler(downwardAngle, 0, 0);
                    transform.rotation = Quaternion.Slerp(startRot, targetRotation, t);
                }
            }
            yield return null;
        }
    }

    IEnumerator MoveAndRotateToPoint(Vector3 startPos, Quaternion startRot, Vector3 toPosition, float moveTime, bool useDownwardAngle)
    {
        float timer = 0f;

        Vector3 targetDirection = connerA.position - toPosition;
        targetDirection.y = 0;
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection.normalized) * Quaternion.Euler(downwardAngle, 0, 0);

        while (timer < moveTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / moveTime);

            transform.position = Vector3.Lerp(startPos, toPosition, t);
            transform.rotation = Quaternion.Slerp(startRot, targetRotation, t);
            yield return null;
        }
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
            float t = Mathf.SmoothStep(0f, 1f, timer / transitionDuration);

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
        
        if (_lockPitch)
        {
            pitch = _lockedPitchValue;
        }
        else
        {
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 desiredPosition = target.position + rotation * offset;

        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.LookAt(target);
    }

    public void SetPitchLock(bool locked, float pitchValue = 0f)
    {
        _lockPitch = locked;
        if (_lockPitch)
        {
            _lockedPitchValue = pitchValue;
        }
    }
    
    // ===================调试函数，下面均可删======================
    // 显示路径函数
    void OnDrawGizmosSelected()
    {
        // 绘制路径点
        DrawPointWithLabel(startPoint, "起点", Color.green);
        DrawPointWithLabel(cookiePosition, "饼干位置", Color.red);
        DrawPointWithLabel(endPoint, "第二个点", Color.blue);
        DrawPointWithLabel(connerA, "拐弯起点", Color.yellow);
        DrawPointWithLabel(connerB, "拐弯终点", Color.magenta);
        DrawPointWithLabel(returnPoint, "返回点", Color.cyan);

        // 绘制路径线
        if (startPoint && cookiePosition)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(startPoint.position, cookiePosition.position);
            DrawArrow(startPoint.position, (cookiePosition.position - startPoint.position).normalized, 1f);
        }
        if (cookiePosition && endPoint)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(cookiePosition.position, endPoint.position);
            DrawArrow(cookiePosition.position, (endPoint.position - cookiePosition.position).normalized, 1f);
        }
        if (endPoint && connerA)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(endPoint.position, connerA.position);
            DrawArrow(endPoint.position, (connerA.position - endPoint.position).normalized, 1f);
        }
        if (connerA && connerB)
        {
            // 绘制水平贝塞尔曲线预览
            DrawHorizontalBezierCurve(connerA.position, connerB.position, Color.yellow);
        }
        if (connerB && returnPoint)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(connerB.position, returnPoint.position);
            DrawArrow(connerB.position, (returnPoint.position - connerB.position).normalized, 1f);
        }
        if (returnPoint && target)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(returnPoint.position, target.position);
            DrawArrow(returnPoint.position, (target.position - returnPoint.position).normalized, 1f);
        }
    }

    // 绘制水平贝塞尔曲线预览
    void DrawHorizontalBezierCurve(Vector3 start, Vector3 end, Color color)
    {
        Vector3 controlPoint = CalculateHorizontalControlPoint(start, end);
        Gizmos.color = color;
        
        // 绘制曲线段
        Vector3 prevPoint = start;
        for (int i = 1; i <= 20; i++)
        {
            float t = i / 20f;
            Vector3 point = CalculateBezierPoint(start, controlPoint, end, t);
            Gizmos.DrawLine(prevPoint, point);
            prevPoint = point;
        }
        
        // 绘制控制点连线
        Gizmos.color = color * 0.7f;
        Gizmos.DrawLine(start, controlPoint);
        Gizmos.DrawLine(controlPoint, end);
        
        // 绘制控制点
        Gizmos.color = Color.white;
        Gizmos.DrawSphere(controlPoint, 0.2f);
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