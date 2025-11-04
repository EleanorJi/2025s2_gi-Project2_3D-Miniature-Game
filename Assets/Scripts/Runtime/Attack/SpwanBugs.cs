using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class SpwanBugs : MonoBehaviour
{
    public static SpwanBugs Instance; // Singleton instance

    public GameObject bugPrefab;                 // Main insect prefab
    public GameObject secondBugPrefab;           // The second type of insect model
    public float bugPrefabChance = 0.5f;         // The probability of the first type of insect appearing (0..1)

    public Transform[] spawnPoints;

    public float minSpeed = 1f;
    public float maxSpeed = 3f;

    [Range(0f, 1f)]
    public float cookieDisplayChance = 0.5f;

    public bool startOnAwake = true;
    
    public bool[] reverseDirection;              // Control whether the direction of each generation point is reversed

    public int maxPrimaryBugs = 5;               // The largest number of the first type of insects
    private int currentPrimaryBugs = 0;          // The current number of the first type of insects on the field

    // Add to the class
    private List<List<GameObject>> spawnedBugsPerPoint = new List<List<GameObject>>();

    void Awake()
    {
        // Singleton pattern initialization
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        
        // Set the value of maxPrimaryBugs to 5
        maxPrimaryBugs = 5;
    }

    void Start()
    {
        if (startOnAwake)
        {
            StartSpawning();
        }
    }

    public void StartSpawning()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            return;
        }

        if (bugPrefab == null && secondBugPrefab == null)
        {
            return;
        }

        // Initialize the direction array
        if (reverseDirection.Length != spawnPoints.Length)
        {
            Array.Resize(ref reverseDirection, spawnPoints.Length);
        }

        // Initialize the list
        spawnedBugsPerPoint.Clear();
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            spawnedBugsPerPoint.Add(new List<GameObject>());
        }

        int count = Mathf.Min(6, spawnPoints.Length);
        for (int i = 0; i < count; i++)
        {
            StartCoroutine(SpawnAtPoint(i));
        }
    }

    IEnumerator SpawnAtPoint(int index)
    {
        Transform spPoint = spawnPoints[index];
        while (true)
        {
            if (spPoint == null)
            {
                yield break;
            }

            //First, a random speed is generated, and the three insects share this speed.
            float sharedSpeed = UnityEngine.Random.Range(minSpeed, maxSpeed);

           
            List<GameObject> currentBatch = new List<GameObject>();

            // Generate three insects one after another, with a 0.5-second interval between each pair.
            for (int i = 0; i < 3; i++)
            {
                GameObject chosenPrefab = secondBugPrefab;

                if (bugPrefab != null && UnityEngine.Random.value < bugPrefabChance && currentPrimaryBugs < maxPrimaryBugs)
                {
                    chosenPrefab = bugPrefab;
                }

                if (chosenPrefab == null)
                {
                    yield break;
                }

                Vector3 pos = spPoint.position;
                Quaternion rot = spPoint.rotation;
                GameObject bug = Instantiate(chosenPrefab, pos, rot);

                // Increase the corresponding count according to the type of the insect.
                if (chosenPrefab == bugPrefab)
                {
                    currentPrimaryBugs++;
                }
               
                currentBatch.Add(bug);

              
                float speed = sharedSpeed;

                // Determine the direction of movement (based on whether it is reversed)
                Vector3 moveDir = spPoint.forward;
                if (reverseDirection[index])
                {
                    moveDir = -moveDir; // Reverse direction
                }
                
                if (moveDir.sqrMagnitude < 0.0001f) moveDir = Vector3.forward;

                bug.transform.rotation = Quaternion.LookRotation(moveDir, Vector3.up);

                Quaternion lookRot = Quaternion.LookRotation(moveDir, Vector3.up);
                if (chosenPrefab == secondBugPrefab)
                {
                    lookRot *= Quaternion.Euler(0f, 0f, -90f);
                }

                bug.transform.rotation = lookRot;

                Rigidbody rb = bug.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = moveDir * speed;
                }

                // Add event listener for destruction
                BugDestroyListener listener = bug.AddComponent<BugDestroyListener>();
                listener.onDestroyed = () => {
                    if (chosenPrefab == bugPrefab)
                    {
                        currentPrimaryBugs = Math.Max(0, currentPrimaryBugs - 1);
                    }
                };

                SetCookieOnBug(bug);

                // There is a 0.5-second interval between each insect.
                if (i < 2) // The first two insects are generated and then wait. The last one doesn't need to wait.
                {
                    yield return new WaitForSeconds(0.5f);
                }
            }

            // Wait for all the insects in the current batch to die.
            while (currentBatch.Count > 0)
            {
                // Check and remove the dead insects
                for (int i = currentBatch.Count - 1; i >= 0; i--)
                {
                    if (currentBatch[i] == null || !currentBatch[i].activeInHierarchy)
                    {
                        currentBatch.RemoveAt(i);
                    }
                }

                // If there are still surviving insects, continue to wait.
                if (currentBatch.Count > 0)
                {
                    yield return new WaitForSeconds(0.5f); // Check every 0.5 seconds
                }
            }

            //Add a short delay before the next batch is generated so that it doesn't generate so quickly
            yield return new WaitForSeconds(1f);
        }
    }

    void SetCookieOnBug(GameObject bug)
    {
        if (bug == null) return;

        GameObject cookieObj = null;
        foreach (Transform t in bug.GetComponentsInChildren<Transform>(true))
        {
            if (t.name.Equals("Cookie", StringComparison.OrdinalIgnoreCase))
            {
                // According to the random probability, it shows
                bool show = UnityEngine.Random.value < cookieDisplayChance;
                t.gameObject.SetActive(show);
                if (show)
                {
                    cookieObj = t.gameObject;
                    // Add following/dropping logic to the Cookie
                    var follower = cookieObj.GetComponent<CookieFollower>();
                    if (follower == null)
                    {
                        follower = cookieObj.AddComponent<CookieFollower>();
                    }
                    follower.target = bug.transform;
                    follower.offset = new Vector3(0f, 0.15f, 0f);
                    follower.enabled = true;
                }
                break;
            }
        }
    }

    // Reduce the count of maxPrimaryBugs
    public void DecreaseMaxPrimaryBugs()
    {

        maxPrimaryBugs = Math.Max(0, maxPrimaryBugs - 1);
        Debug.Log($"Max primary bugs decreased. New value: {maxPrimaryBugs}");
    }
}

// Auxiliary component, used to listen for the event of bug destruction
public class BugDestroyListener : MonoBehaviour
{
    public System.Action onDestroyed;
    
    void OnDestroy()
    {
        if (onDestroyed != null)
        {
            onDestroyed();
        }
    }
}