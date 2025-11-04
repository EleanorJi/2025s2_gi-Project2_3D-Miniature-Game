using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Scene Sequence Manager - Provides the function of sequentially jumping to scenes
/// </summary>
public class SceneOrderManager : MonoBehaviour
{
    private static SceneOrderManager _instance;
    public static SceneOrderManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("SceneOrderManager");
                _instance = go.AddComponent<SceneOrderManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // Scene sequence list - Editable in Inspector
    [Header("Scene sequence setting")]
    [SerializeField] private string[] scenes = new string[]
    {
        "StartScene",
        "Level0_Tutorial",
        "Level1_kitchen", 
        "Level2_Street",
        "Level3_boss"
    };

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Load the next scene
    /// </summary>
    public void LoadNextScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        int currentIndex = GetSceneIndex(currentScene);
        
        if (currentIndex >= 0 && currentIndex < scenes.Length - 1)
        {
            string nextScene = scenes[currentIndex + 1];
            Debug.Log($"from {currentScene} to {nextScene}");
            SceneManager.LoadScene(nextScene);
        }
        else
        {
            Debug.Log($"It is now the final scene: {currentScene}");
            // Can choose to return to the first scene or follow another logic.
            SceneManager.LoadScene(scenes[0]);
        }
    }

    /// <summary>
    /// Load the previous scene
    /// </summary>
    public void LoadPreviousScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        int currentIndex = GetSceneIndex(currentScene);
        
        if (currentIndex > 0)
        {
            string previousScene = scenes[currentIndex - 1];
            Debug.Log($"from {currentScene} to {previousScene}");
            SceneManager.LoadScene(previousScene);
        }
        else
        {
            Debug.Log($"It is already the first scene: {currentScene}");
        }
    }

    /// <summary>
    /// Obtain the index of the current scene in the sequence
    /// </summary>
    private int GetSceneIndex(string sceneName)
    {
        for (int i = 0; i < scenes.Length; i++)
        {
            // Support full path and simple name matching
            if (scenes[i] == sceneName || 
                scenes[i].EndsWith("/" + sceneName) ||
                sceneName.EndsWith("/" + scenes[i]))
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Get the name of the next scene (without loading)
    /// </summary>
    public string GetNextSceneName()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        int currentIndex = GetSceneIndex(currentScene);
        
        if (currentIndex >= 0 && currentIndex < scenes.Length - 1)
        {
            return scenes[currentIndex + 1];
        }
        return scenes[0]; // Loop back to the first one
    }

    /// <summary>
    /// Set the new scene sequence
    /// </summary>
    public void SetSceneOrder(string[] newOrder)
    {
        if (newOrder != null && newOrder.Length > 0)
        {
            System.Array.Copy(newOrder, scenes, Mathf.Min(newOrder.Length, scenes.Length));
            Debug.Log("The scene sequence has been updated.");
        }
    }

    /// <summary>
    /// Obtain the current scene sequence
    /// </summary>
    public string[] GetSceneOrder()
    {
        return (string[])scenes.Clone();
    }

    /// <summary>
    /// Display the current scene information in the Unity Inspector
    /// </summary>
    [ContextMenu("Display current scene information")]
    public void ShowCurrentSceneInfo()
    {
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        int currentIndex = GetSceneIndex(currentScene);
        
        Debug.Log($"=== Scene sequence information ===");
        Debug.Log($"Current scene: {currentScene} (index: {currentIndex})");
        Debug.Log($"Total scene: {scenes.Length}");
        Debug.Log($"Sequence of Scenes:");
        for (int i = 0; i < scenes.Length; i++)
        {
            string marker = (i == currentIndex) ? " <- current" : "";
            Debug.Log($"  {i}: {scenes[i]}{marker}");
        }
        
        if (currentIndex >= 0 && currentIndex < scenes.Length - 1)
        {
            Debug.Log($"next scene: {scenes[currentIndex + 1]}");
        }
        else
        {
            Debug.Log($"next scene: {scenes[0]} (Loop back to the beginning)");
        }
    }

    /// <summary>
    /// Test: Jump to the next scene (only in the editor)
    /// </summary>
    [ContextMenu("Test to jump to the next scene")]
    public void TestLoadNextScene()
    {
        #if UNITY_EDITOR
        if (Application.isPlaying)
        {
            LoadNextScene();
        }
        else
        {
            Debug.LogWarning("Please test the scene transition in the playback mode.");
        }
        #endif
    }
}
