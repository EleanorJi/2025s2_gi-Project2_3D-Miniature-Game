using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }
    
    private List<Checkpoint> allCheckpoints = new List<Checkpoint>();
    private Checkpoint lastActivatedCheckpoint; // The last activated save point
    private List<Checkpoint> activatedCheckpoints = new List<Checkpoint>(); // All activated save points
    
    void Awake()
    {
        // Singleton Pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // Monitor the scene loading process, clean up and rebuild the checkpoint list to avoid holding references to objects that have been destroyed.
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // Automatically search for all save points in the scene
        FindAllCheckpoints();
    }
    
    private void FindAllCheckpoints()
    {
        allCheckpoints.Clear();
        var checkpoints = FindObjectsByType<Checkpoint>(FindObjectsSortMode.InstanceID);
        allCheckpoints.AddRange(checkpoints);
        
        Debug.Log($"Found {allCheckpoints.Count} checkpoints in scene");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // After the scene change, clear the activated ones and the last checkpoint, and rebuild the checkpoint list for the current scene
        activatedCheckpoints.RemoveAll(cp => cp == null);
        activatedCheckpoints.Clear();
        lastActivatedCheckpoint = null;
        FindAllCheckpoints();
    }
    
    public void SetCheckpointActivated(Checkpoint checkpoint)
    {
        // If this save point is not already in the activation list, add it.
        if (!activatedCheckpoints.Contains(checkpoint))
        {
            activatedCheckpoints.Add(checkpoint);
        }
        
        // Update to the last activated save point
        lastActivatedCheckpoint = checkpoint;
        Debug.Log($"Current respawn point set to: {checkpoint.gameObject.name}");
    }
    
    public Vector3 GetLastRespawnPosition()
    {
        // Directly return to the last activated save point location
        if (lastActivatedCheckpoint != null)
        {
            // Defense: The object may have been destroyed (Unity considers it null)
            if (lastActivatedCheckpoint == null)
            {
                lastActivatedCheckpoint = null;
            }
            else
            {
                return lastActivatedCheckpoint.transform.position;
            }
        }
        
        // If there is no active save point, return to the first save point or the default location.
        if (allCheckpoints.Count > 0)
        {
            // Remove the destroyed elements
            allCheckpoints.RemoveAll(cp => cp == null);
            if (allCheckpoints.Count > 0)
                return allCheckpoints[0].transform.position;
        }
        
        return Vector3.zero;
    }
    
    // Retrieve all the activated save points
    public List<Checkpoint> GetActivatedCheckpoints()
    {
        return new List<Checkpoint>(activatedCheckpoints);
    }
    
    // Obtain the last activated save point
    public Checkpoint GetLastCheckpoint()
    {
        return lastActivatedCheckpoint;
    }
    
    // Manually set the save point (ignoring the repeated activation limit)
    public void SetCheckpointManually(Checkpoint checkpoint)
    {
        checkpoint.ForceActivate();
    }
    
    // Manually register the archive point
    public void RegisterCheckpoint(Checkpoint checkpoint)
    {
        if (!allCheckpoints.Contains(checkpoint))
        {
            allCheckpoints.Add(checkpoint);
        }
    }
    
    // Manually cancel the registration archive point
    public void UnregisterCheckpoint(Checkpoint checkpoint)
    {
        allCheckpoints.Remove(checkpoint);
        activatedCheckpoints.Remove(checkpoint);
        
        if (lastActivatedCheckpoint == checkpoint)
        {
            // If the current saved point is removed, revert to the previous activated saved point.
            lastActivatedCheckpoint = activatedCheckpoints.Count > 0 ? 
                activatedCheckpoints[activatedCheckpoints.Count - 1] : null;
        }
    }
    
    // Reset the status of all save points
    public void ResetAllCheckpoints()
    {
        foreach (Checkpoint checkpoint in allCheckpoints)
        {
            if (checkpoint != null)
                checkpoint.ResetCheckpoint();
        }
        activatedCheckpoints.RemoveAll(cp => cp == null);
        activatedCheckpoints.Clear();
        lastActivatedCheckpoint = null;
    }
    
    // Debug
    public void DebugActivatedCheckpoints()
    {
        Debug.Log($"Total activated checkpoints: {activatedCheckpoints.Count}");
        for (int i = 0; i < activatedCheckpoints.Count; i++)
        {
            var checkpoint = activatedCheckpoints[i];
            if (checkpoint == null)
                continue;
            var go = checkpoint.gameObject; // Defensive access
            if (go == null)
                continue;
            Debug.Log($" - {go.name} (Last: {checkpoint == lastActivatedCheckpoint})");
        }
    }
}