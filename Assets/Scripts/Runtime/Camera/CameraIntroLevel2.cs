using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CameraIntroLevel2 : MonoBehaviour
{
    [Header("Level2 Opening Animation Points")]
    public Transform startPoint;           // strrt point
    public Transform endPoint;             // endpoint
    public Transform lookAtTarget;         // look at target

    [Header("Player Settings")]
    public GameObject playerObject;        // The player object to be hidden

    [Header("Animation Settings")]
    public Animator targetAnimator;        // The Animator component of the target object
    public string animationTrigger = "IsLevel2"; // Animation trigger name
    public GameObject targetObject;        // The target object to be hidden

    [Header("Time Settings")]
    public float moveTime = 1.5f;          // move time
    public float stayTime = 1f;            // stay time
    public float fadeInTime = 1f;          // Black fade-in time
    public float fadeInDelay = 0f;         // The delay time before the fade-in begins

    [Header("Fade Settings")]
    public Image blackFadeImage;           // Black UI panel

    private CameraFollow cameraFollow;
    private PlayerInputController playerInputController;
    private bool isIntroPlaying = false;

    public bool IsIntroPlaying => isIntroPlaying;

    void Start()
    {
        cameraFollow = GetComponent<CameraFollow>();
        if (cameraFollow == null)
        {
            Debug.LogError("CameraIntroLevel2 need CameraFollow component!");
            return;
        }

        playerInputController = FindAnyObjectByType<PlayerInputController>();
        if (playerInputController == null)
        {
            Debug.LogWarning("PlayerInputController not found!");
            return;
        }

        // Initialize the black panel
        if (blackFadeImage != null)
        {
            // It was initially set to be completely opaque (all black)
            Color startColor = blackFadeImage.color;
            startColor.a = 1f;
            blackFadeImage.color = startColor;
            blackFadeImage.gameObject.SetActive(true);
        }

        // Check if the path points have been set up
        if (startPoint != null && endPoint != null && lookAtTarget != null)
        {
            StartCoroutine(PlayIntroAnimation());
        }
        else
        {
            Debug.LogWarning("Missing path points, no Level2 opening animation");
            // If no points are set, enable player control directly.
            StartCoroutine(FadeInOnly());
        }
    }

    public IEnumerator PlayIntroAnimation()
    {
        isIntroPlaying = true;
        
        // Hide player objects
        if (playerObject != null)
            playerObject.SetActive(false);

        // Trigger the animation of the target
        if (targetAnimator != null && !string.IsNullOrEmpty(animationTrigger))
        {
            targetAnimator.SetTrigger(animationTrigger);
        }

        // Disable player input and camera follow-up
        playerInputController.DisableInput();
        cameraFollow.SetCameraControl(false);

        // Set the starting position and orientation
        transform.position = startPoint.position;
        
        // Look directly at the target point immediately.
        if (lookAtTarget != null)
        {
            Vector3 direction = lookAtTarget.position - transform.position;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        // At the same time, the fade-in effect and the movement animation start.
        yield return StartCoroutine(PlayFadeAndMoveSimultaneously());
        
        // Stay for a while
        yield return new WaitForSeconds(stayTime);
        
        // Smoothly transition to the player's perspective
        yield return StartCoroutine(SmoothTransitionToPlayer());

        // When the opening animation ends: Hide the target object and display the Player
        if (targetObject != null)
            targetObject.SetActive(false);
            
        if (playerObject != null)
            playerObject.SetActive(true);

        // Re-enable player input and camera follow-up
        cameraFollow.SetCameraControl(true);
        playerInputController.EnableInput();
        
        isIntroPlaying = false;
    }

    IEnumerator PlayFadeAndMoveSimultaneously()
    {
        // Start both the fade-in and the movement coroutines simultaneously
        Coroutine fadeCoroutine = StartCoroutine(FadeInBlackScreen());
        Coroutine moveCoroutine = StartCoroutine(MoveToEndPoint());

        // Wait for both animations to be completed
        yield return fadeCoroutine;
        yield return moveCoroutine;
    }

    IEnumerator FadeInBlackScreen()
    {
        if (blackFadeImage == null) yield break;

        // Delay for a while before starting the fade-in effect
        if (fadeInDelay > 0)
        {
            yield return new WaitForSeconds(fadeInDelay);
        }

        float timer = 0f;
        Color color = blackFadeImage.color;
        float startAlpha = color.a;

        while (timer < fadeInTime)
        {
            timer += Time.deltaTime;
            float t = timer / fadeInTime; // Use linear interpolation and keep it consistent with EndLevel1.
            
            // From opaque gradient to transparency (black disappears)
            color.a = Mathf.Lerp(startAlpha, 0f, t);
            blackFadeImage.color = color;
            
            yield return null;
        }

        // Ensure complete transparency in the final outcome.
        color.a = 0f;
        blackFadeImage.color = color;
        
        // Disable the UI completely after it becomes fully transparent to enhance performance.
        blackFadeImage.gameObject.SetActive(false);
    }

    IEnumerator FadeInOnly()
    {
        // Only perform the fade-in effect, and then enable the control.
        yield return StartCoroutine(FadeInBlackScreen());
        
        cameraFollow.SetCameraControl(true);
        if (playerInputController != null)
            playerInputController.EnableInput();
    }

    IEnumerator MoveToEndPoint()
    {
        float timer = 0f;
        Vector3 startPos = transform.position;

        while (timer < moveTime)
        {
            timer += Time.deltaTime;
            float t = timer / moveTime; // Use linear interpolation and keep it consistent with EndLevel1.

            transform.position = Vector3.Lerp(startPos, endPoint.position, t);

            if (lookAtTarget != null)
            {
                Vector3 currentDirection = lookAtTarget.position - transform.position;
                if (currentDirection != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(currentDirection);
                }
            }

            yield return null;
        }

        transform.position = endPoint.position;
        
        if (lookAtTarget != null)
        {
            Vector3 finalDirection = lookAtTarget.position - transform.position;
            if (finalDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(finalDirection);
            }
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
            float t = timer / transitionDuration; // Use linear interpolation and keep it consistent with EndLevel1.

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

    // Fade-out effect for use in other places
    public IEnumerator FadeOutBlackScreen(float duration = 1f, float delay = 0f)
    {
        if (blackFadeImage == null) yield break;

        // 延迟
        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }

        blackFadeImage.gameObject.SetActive(true);
        
        float timer = 0f;
        Color color = blackFadeImage.color;
        float startAlpha = color.a;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            
            color.a = Mathf.Lerp(startAlpha, 1f, t);
            blackFadeImage.color = color;
            
            yield return null;
        }

        color.a = 1f;
        blackFadeImage.color = color;
    }
}