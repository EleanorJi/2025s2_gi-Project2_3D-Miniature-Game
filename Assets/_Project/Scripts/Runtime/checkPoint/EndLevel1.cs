using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

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

    [Header("Level settings")]
    public string nextLevelName = "Level2"; // The name of the scene for the next level
    public float totalSequenceTime = 4f;   // The total time of the entire ending sequence (camera movement + animation playback)
    private PlayerInputController playerInputController;
    private CameraFollow cameraFollow;
    private Animator endAnimator;          // The Animator component of the End object
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

        // 获取End物体的Animator组件
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

        // If there is still time left, wait until the animation finishes playing.
        if (remainingTime > 0)
        {
            Debug.Log("Waiting for the animation to finish playing. Remaining time:" + remainingTime.ToString("F2") + "seconds");
            yield return new WaitForSeconds(remainingTime);
        }
        else
        {
            Debug.LogWarning("The total time setting might be too short. Load the next level immediately.");
        }

        Debug.Log("Sequence completed. Loading next level.");
        LoadNextLevel();
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

    void LoadNextLevel()
    {
        Debug.Log("Load the next level: 使用场景顺序跳转");
        
        // Load by using the scene order
        SceneOrderManager.Instance.LoadNextScene();
    }
}