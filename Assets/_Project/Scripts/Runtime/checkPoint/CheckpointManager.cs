using UnityEngine;
using System.Collections.Generic;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }
    
    private List<Checkpoint> allCheckpoints = new List<Checkpoint>();
    private Checkpoint lastActivatedCheckpoint; // 最后一个激活的存档点
    private List<Checkpoint> activatedCheckpoints = new List<Checkpoint>(); // 所有激活过的存档点
    
    void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // 自动查找场景中所有存档点
        FindAllCheckpoints();
    }
    
    private void FindAllCheckpoints()
    {
        Checkpoint[] checkpoints = FindObjectsByType<Checkpoint>(FindObjectsSortMode.InstanceID);
        allCheckpoints.AddRange(checkpoints);
        
        Debug.Log($"Found {allCheckpoints.Count} checkpoints in scene");
    }
    
    public void SetCheckpointActivated(Checkpoint checkpoint)
    {
        // 如果这个存档点还没有在激活列表中，添加它
        if (!activatedCheckpoints.Contains(checkpoint))
        {
            activatedCheckpoints.Add(checkpoint);
        }
        
        // 更新为最后一个激活的存档点
        lastActivatedCheckpoint = checkpoint;
        Debug.Log($"Current respawn point set to: {checkpoint.gameObject.name}");
    }
    
    public Vector3 GetLastRespawnPosition()
    {
        // 直接返回最后一个激活的存档点位置
        if (lastActivatedCheckpoint != null)
        {
            return lastActivatedCheckpoint.transform.position;
        }
        
        // 如果没有激活的存档点，返回第一个存档点或默认位置
        if (allCheckpoints.Count > 0)
        {
            return allCheckpoints[0].transform.position;
        }
        
        return Vector3.zero;
    }
    
    // 获取所有激活过的存档点
    public List<Checkpoint> GetActivatedCheckpoints()
    {
        return new List<Checkpoint>(activatedCheckpoints);
    }
    
    // 获取最后一个激活的存档点
    public Checkpoint GetLastCheckpoint()
    {
        return lastActivatedCheckpoint;
    }
    
    // 手动设置存档点（忽略重复激活限制）
    public void SetCheckpointManually(Checkpoint checkpoint)
    {
        checkpoint.ForceActivate();
    }
    
    // 手动注册存档点
    public void RegisterCheckpoint(Checkpoint checkpoint)
    {
        if (!allCheckpoints.Contains(checkpoint))
        {
            allCheckpoints.Add(checkpoint);
        }
    }
    
    // 手动取消注册存档点
    public void UnregisterCheckpoint(Checkpoint checkpoint)
    {
        allCheckpoints.Remove(checkpoint);
        activatedCheckpoints.Remove(checkpoint);
        
        if (lastActivatedCheckpoint == checkpoint)
        {
            // 如果移除的是当前存档点，回退到上一个激活的存档点
            lastActivatedCheckpoint = activatedCheckpoints.Count > 0 ? 
                activatedCheckpoints[activatedCheckpoints.Count - 1] : null;
        }
    }
    
    // 重置所有存档点状态（用于新游戏等）
    public void ResetAllCheckpoints()
    {
        foreach (Checkpoint checkpoint in allCheckpoints)
        {
            checkpoint.ResetCheckpoint();
        }
        activatedCheckpoints.Clear();
        lastActivatedCheckpoint = null;
        
        // 重新激活第一个存档点
        if (allCheckpoints.Count > 0)
        {
            allCheckpoints[0].ForceActivate();
        }
    }
    
    // 调试方法
    public void DebugActivatedCheckpoints()
    {
        Debug.Log($"Total activated checkpoints: {activatedCheckpoints.Count}");
        foreach (Checkpoint checkpoint in activatedCheckpoints)
        {
            Debug.Log($" - {checkpoint.gameObject.name} (Last: {checkpoint == lastActivatedCheckpoint})");
        }
    }
}