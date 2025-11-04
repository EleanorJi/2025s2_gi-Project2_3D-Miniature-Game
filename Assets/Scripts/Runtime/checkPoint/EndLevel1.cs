using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndLevel1 : MonoBehaviour
{
    [Header("Player Settings")]
    public GameObject player;
    
    [Header("Camera transition settings")]
    public Transform cameraEndPosition;    // The position where the camera has moved to (an empty object)
    public Transform cameraLookAtTarget;   // The position that the camera is pointing at (an empty object)
    public float transitionDuration = 2f;  // Camera movement time

    [Header("Animation control")]
    public GameObject endAnimationObject;  // Drag the End object with Animator attached here
    public GameObject nextAnimationObject;  

    [Header("Level settings")]
    public string nextLevelName = "Level2"; // The name of the scene for the next level
    public float totalSequenceTime = 4f;   // The total time of the entire ending sequence (camera movement + animation playback)
    
    [Header("Fade Out Settings")]
    public Image blackFadePanel;           // Black fading panel
    public float fadeOutDuration = 2f;     // Fade-out duration
    public float fadeOutDelay = 1f;        // The delay time before the fade-out begins

    private PlayerInputController playerInputController;
    private CameraFollow cameraFollow;
    private Animator endAnimator;          // The Animator component of the End object
    private Animator nextAnimator;
    private bool hasTriggered = false; // Prevent repeated triggering

    void Start()
    {
        // If the player object has not been manually specified, attempt to automatically search for it.
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }

        if (player != null)
        {
            playerInputController = player.GetComponent<PlayerInputController>();
            cameraFollow = Camera.main.GetComponent<CameraFollow>();
            
            if (playerInputController == null)
            {
                Debug.LogWarning("[EndLevel1] Cannot find PlayerInputController component");
            }
            
            if (cameraFollow == null)
            {
                Debug.LogError("The CameraFollow component cannot be found on the main camera.");
            }
        }
        else
        {
            Debug.LogError("Cannot find the object with the Player tag");
        }

        // Obtain the Animator component of the End object
        if (endAnimationObject != null)
        {
            endAnimator = endAnimationObject.GetComponent<Animator>();
            if (endAnimator == null)
            {
                Debug.LogError("The Animator component cannot be found on the End object.");
            }
        }
        else
        {
            Debug.LogError("need set End Animation Object");
        }
        if (nextAnimationObject != null)
        {
            nextAnimator = nextAnimationObject.GetComponent<Animator>();
            if (nextAnimator == null)
            {
                Debug.LogError("The Animator component cannot be found on the next animation object.");
            }
        }

        // Initialize the black panel (make sure it is hidden at the beginning)
        if (blackFadePanel != null)
        {
            Color color = blackFadePanel.color;
            color.a = 0f;
            blackFadePanel.color = color;
            blackFadePanel.gameObject.SetActive(false);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if it was triggered by the player and has not been triggered before.
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true;
            EndLevelSequence();
        }
    }

    void EndLevelSequence()
    {
        Debug.Log("Start the level ending sequence. Total duration: " + totalSequenceTime + "seconds");
        
        // Set the IsEnd parameter of the animation to true
        if (endAnimator != null)
        {
            endAnimator.SetBool("IsEnd", true);
        }
        
        // Disable player input and camera follow-up
        if (playerInputController != null)
        {
            playerInputController.DisableInput();
        }
        
        if (cameraFollow != null)
        {
            cameraFollow.SetCameraControl(false);
        }
        
        // Start the end sequence coroutine
        StartCoroutine(EndSequenceCoroutine());
    }

    IEnumerator EndSequenceCoroutine()
    {
        // Record start time
        float sequenceStartTime = Time.time;

        // If there is a camera transition setting, execute the camera movement
        if (cameraEndPosition != null && cameraLookAtTarget != null)
        {
            yield return StartCoroutine(CameraTransition());
        }
        else
        {
            // If there is no camera cut, simply wait for the camera movement time to pass.
            yield return new WaitForSeconds(transitionDuration);
        }

        // Calculate the remaining time that needs to be waited.
        float elapsedTime = Time.time - sequenceStartTime;
        float remainingTime = totalSequenceTime - elapsedTime;

        if (endAnimator != null && endAnimationObject != null)
        {
            // Wait for the animation state to finish playing.
            yield return StartCoroutine(WaitForAnimationToFinish(endAnimator));

            Debug.Log("First animation finished playing. Hiding the first animation object.");
            endAnimationObject.SetActive(false);
        }

        if (nextAnimator != null)
        {
            Debug.Log("Triggering the second animation's IsEnd parameter...");
            nextAnimator.SetBool("IsEnd", true);
            yield return new WaitForSeconds(2f); //Wait for the second animation to play for 2 seconds.
        }

        // Perform a black fade-out effect before loading the next level.
        yield return StartCoroutine(FadeOutBlackScreen());

        Debug.Log("Sequence completed. Loading next level.");
        LoadNextLevel();
    }

    // Wait for the animation to finish playing.
    IEnumerator WaitForAnimationToFinish(Animator animator)
    {
        // Wait for one frame to ensure that the animation state has been updated
        yield return null;

        // Obtain the current playback status information of the animation
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        // Wait for the animation to finish playing.
        while (stateInfo.normalizedTime < 1.0f)
        {
            yield return null;
            stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        }

        Debug.Log("Animation finished playing.");
    }

    IEnumerator CameraTransition()
    {
        float timer = 0f;
        Vector3 startPosition = cameraFollow.transform.position;
        Quaternion startRotation = cameraFollow.transform.rotation;

        // Calculate target rotation: Look towards the target position
        Vector3 lookDirection = cameraLookAtTarget.position - cameraEndPosition.position;
        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);

        while (timer < transitionDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / transitionDuration);

            // Smoothly move the position
            cameraFollow.transform.position = Vector3.Lerp(startPosition, cameraEndPosition.position, t);
            
            // Smooth rotation of the view
            cameraFollow.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            yield return null;
        }

        // Ensure the final position and rotation are accurate
        cameraFollow.transform.position = cameraEndPosition.position;
        cameraFollow.transform.rotation = targetRotation;

        Debug.Log("Camera transition completed");
    }

    IEnumerator FadeOutBlackScreen()
    {
        if (blackFadePanel == null)
        {
            Debug.LogWarning("BlackFadePanel is not assigned!");
            yield break;
        }

        // Delay for a while before starting to fade out
        if (fadeOutDelay > 0)
        {
            yield return new WaitForSeconds(fadeOutDelay);
        }

        // Activate the black panel
        blackFadePanel.gameObject.SetActive(true);

        float timer = 0f;
        Color color = blackFadePanel.color;
        float startAlpha = color.a; // 0

        while (timer < fadeOutDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / fadeOutDuration);
            
            // From transparent gradient to opaque (black)
            color.a = Mathf.Lerp(startAlpha, 1f, t);
            blackFadePanel.color = color;
            
            yield return null;
        }

        // Ensure that it is completely opaque in the end.
        color.a = 1f;
        blackFadePanel.color = color;

        Debug.Log("Fade out completed");
    }

    void LoadNextLevel()
    {
        Debug.Log("Load the next level: Use scene sequence navigation");
        
        // Load by using the scene order
        SceneOrderManager.Instance.LoadNextScene();
    }

    // Used to trigger the fade-out effect in other places
    public void TriggerFadeOut()
    {
        StartCoroutine(FadeOutBlackScreen());
    }
}