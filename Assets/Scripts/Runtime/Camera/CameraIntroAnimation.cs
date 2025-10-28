using UnityEngine;
using System.Collections;

public class CameraIntroAnimation : MonoBehaviour
{
    [Header("Opening animation path points")]
    public Transform startPoint;           
    public Transform cookiePosition;          
    public Transform endPoint;          
    public Transform connerA;          
    public Transform connerB;          
    public Transform returnPoint;

    [Header("Time settings for each stage")]
    public float startToCookieRotateTime = 1f;
    public float startToCookieMoveTime = 3f;
    public float cookieStayTime = 1f;
    public float cookieToSecondTime = 2f;
    public float secondToThirdATime = 5f;
    public float thirdCurveTime = 3f;
    public float thirdToReturnTime = 5f;
    public float returnToPlayerTime = 1f;
    public float downwardAngle = 45f;

    private CameraFollow cameraFollow;
    private PlayerInputController playerInputController;
    private bool isIntroPlaying = false;

    public bool IsIntroPlaying => isIntroPlaying;

    void Start()
    {
        cameraFollow = GetComponent<CameraFollow>();
        if (cameraFollow == null)
        {
            Debug.LogError("CameraIntroAnimation need CameraFollow component!");
            return;
        }

        playerInputController = FindAnyObjectByType<PlayerInputController>();
        if (playerInputController == null)
        {
            return;
        }

        // Check whether all path points have been set up
        if (startPoint != null && cookiePosition != null && endPoint != null && 
            connerA != null && connerB != null && returnPoint != null)
        {
            StartCoroutine(PlayIntroAnimation());
        }
        else
        {
            Debug.LogWarning("missing path, no level1 opening animation");
        }
    }

    public IEnumerator PlayIntroAnimation()
    {
        isIntroPlaying = true;
        
        // Disable player input and camera follow-up
        playerInputController.DisableInput();
        cameraFollow.SetCameraControl(false);

        // Phase 1: Start → Cookie
        transform.position = startPoint.position;

        yield return StartCoroutine(RotateToPoint(cookiePosition.position, startToCookieRotateTime));
        yield return StartCoroutine(MoveToPoint(cookiePosition.position, startToCookieMoveTime, false));
        yield return new WaitForSeconds(cookieStayTime);

        // Stage 2: Cookies → The second item
        yield return StartCoroutine(MoveAndRotateToPoint(endPoint.position, cookieToSecondTime, true));

        // Stage 3-5: Continuous Movement
        yield return StartCoroutine(ContinuousMoveStage3To5(
            endPoint.position, 
            connerA.position,
            connerB.position,
            returnPoint.position,
            secondToThirdATime + thirdCurveTime + thirdToReturnTime
        ));

        // Stage 6: Return Point → Player
        yield return StartCoroutine(ReturnToPlayer(returnToPlayerTime));

        // Finally, it transitions to the player's perspective.
        yield return StartCoroutine(SmoothTransitionToPlayer());

        // Re-enable player input and camera follow-up
        cameraFollow.SetCameraControl(true);
        playerInputController.EnableInput();
        
        isIntroPlaying = false;
    }

    // Coroutine method
    IEnumerator ContinuousMoveStage3To5(Vector3 stage3Start, Vector3 stage4Start, Vector3 stage4End, Vector3 stage5End, float totalTime)
    {
        float timer = 0f;
        
        float stage3EndTime = secondToThirdATime;
        float stage4EndTime = stage3EndTime + thirdCurveTime;
        float stage5EndTime = totalTime;
        
        Quaternion stage3StartRot = transform.rotation;
        Quaternion stage3TargetRot = Quaternion.LookRotation((stage4Start - stage3Start).normalized) * Quaternion.Euler(downwardAngle, 0, 0);
        Quaternion stage4TargetRot = Quaternion.LookRotation((stage5End - stage4End).normalized) * Quaternion.Euler(downwardAngle, 0, 0);
        Quaternion stage5TargetRot = stage4TargetRot;

        while (timer < totalTime)
        {
            timer += Time.deltaTime;
            
            if (timer <= stage3EndTime)
            {
                float t = timer / stage3EndTime;
                transform.position = Vector3.Lerp(stage3Start, stage4Start, t);
                transform.rotation = Quaternion.Lerp(stage3StartRot, stage3TargetRot, t);
            }
            else if (timer <= stage4EndTime)
            {
                float t = (timer - stage3EndTime) / thirdCurveTime;
                Vector3 controlPoint = CalculateHorizontalControlPoint(stage4Start, stage4End);
                transform.position = CalculateBezierPoint(stage4Start, controlPoint, stage4End, t);
                transform.rotation = Quaternion.Lerp(stage3TargetRot, stage4TargetRot, t);
            }
            else
            {
                float t = (timer - stage4EndTime) / thirdToReturnTime;
                transform.position = Vector3.Lerp(stage4End, stage5End, t);
                transform.rotation = Quaternion.Lerp(stage4TargetRot, stage5TargetRot, t);
            }

            yield return null;
        }
        
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

        Transform target = cameraFollow.Target;
        if (target == null) yield break;

        while (timer < moveTime)
        {
            timer += Time.deltaTime;
            float t = timer / moveTime;
            t = Mathf.SmoothStep(0f, 1f, t);

            Quaternion targetRotation = Quaternion.Euler(cameraFollow.Pitch, cameraFollow.Yaw, 0);
            Vector3 targetPosition = target.position + targetRotation * cameraFollow.Offset;

            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            
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

    IEnumerator MoveToPoint(Vector3 toPosition, float moveTime, bool keepDownwardAngle)
    {
        float timer = 0f;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

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

    IEnumerator MoveAndRotateToPoint(Vector3 toPosition, float moveTime, bool useDownwardAngle)
    {
        float timer = 0f;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

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

        Transform target = cameraFollow.Target;
        if (target == null) yield break;

        while (timer < transitionDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / transitionDuration);

            Quaternion targetRot = Quaternion.Euler(cameraFollow.Pitch, cameraFollow.Yaw, 0);
            Vector3 targetPos = target.position + targetRot * cameraFollow.Offset;

            transform.position = Vector3.Lerp(startPosition, targetPos, t);

            Vector3 lookDir = target.position - transform.position;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(startRotation, Quaternion.LookRotation(lookDir), t);
            }
            yield return null;
        }
    }
}