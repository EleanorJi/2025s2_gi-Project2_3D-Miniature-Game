using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景顺序管理器 - 提供按顺序跳转场景的功能
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

    // 场景顺序列表 - 可在Inspector中编辑
    [Header("场景顺序设置")]
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
    /// 加载下一个场景
    /// </summary>
    public void LoadNextScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        int currentIndex = GetSceneIndex(currentScene);
        
        if (currentIndex >= 0 && currentIndex < scenes.Length - 1)
        {
            string nextScene = scenes[currentIndex + 1];
            Debug.Log($"从 {currentScene} 跳转到 {nextScene}");
            SceneManager.LoadScene(nextScene);
        }
        else
        {
            Debug.Log($"已经是最后一个场景: {currentScene}");
            // 可以选择回到第一个场景或者其他逻辑
            SceneManager.LoadScene(scenes[0]);
        }
    }

    /// <summary>
    /// 加载上一个场景
    /// </summary>
    public void LoadPreviousScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        int currentIndex = GetSceneIndex(currentScene);
        
        if (currentIndex > 0)
        {
            string previousScene = scenes[currentIndex - 1];
            Debug.Log($"从 {currentScene} 跳转到 {previousScene}");
            SceneManager.LoadScene(previousScene);
        }
        else
        {
            Debug.Log($"已经是第一个场景: {currentScene}");
        }
    }

    /// <summary>
    /// 获取当前场景在顺序中的索引
    /// </summary>
    private int GetSceneIndex(string sceneName)
    {
        for (int i = 0; i < scenes.Length; i++)
        {
            // 支持完整路径和简单名称匹配
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
    /// 获取下一个场景名称（不加载）
    /// </summary>
    public string GetNextSceneName()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        int currentIndex = GetSceneIndex(currentScene);
        
        if (currentIndex >= 0 && currentIndex < scenes.Length - 1)
        {
            return scenes[currentIndex + 1];
        }
        return scenes[0]; // 循环到第一个
    }

    /// <summary>
    /// 设置新的场景顺序
    /// </summary>
    public void SetSceneOrder(string[] newOrder)
    {
        if (newOrder != null && newOrder.Length > 0)
        {
            System.Array.Copy(newOrder, scenes, Mathf.Min(newOrder.Length, scenes.Length));
            Debug.Log("场景顺序已更新");
        }
    }

    /// <summary>
    /// 获取当前场景顺序
    /// </summary>
    public string[] GetSceneOrder()
    {
        return (string[])scenes.Clone();
    }

    /// <summary>
    /// 在Unity Inspector中显示当前场景信息
    /// </summary>
    [ContextMenu("显示当前场景信息")]
    public void ShowCurrentSceneInfo()
    {
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        int currentIndex = GetSceneIndex(currentScene);
        
        Debug.Log($"=== 场景顺序信息 ===");
        Debug.Log($"当前场景: {currentScene} (索引: {currentIndex})");
        Debug.Log($"场景总数: {scenes.Length}");
        Debug.Log($"场景顺序:");
        for (int i = 0; i < scenes.Length; i++)
        {
            string marker = (i == currentIndex) ? " <- 当前" : "";
            Debug.Log($"  {i}: {scenes[i]}{marker}");
        }
        
        if (currentIndex >= 0 && currentIndex < scenes.Length - 1)
        {
            Debug.Log($"下一个场景: {scenes[currentIndex + 1]}");
        }
        else
        {
            Debug.Log($"下一个场景: {scenes[0]} (循环到开始)");
        }
    }

    /// <summary>
    /// 测试跳转到下一个场景（仅在编辑器中）
    /// </summary>
    [ContextMenu("测试跳转下一场景")]
    public void TestLoadNextScene()
    {
        #if UNITY_EDITOR
        if (Application.isPlaying)
        {
            LoadNextScene();
        }
        else
        {
            Debug.LogWarning("请在播放模式下测试场景跳转");
        }
        #endif
    }
}
