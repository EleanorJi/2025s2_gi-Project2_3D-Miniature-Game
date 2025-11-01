using System.Collections.Generic;
using UnityEngine;

public class ItemRefresh : MonoBehaviour
{
    public Transform[] point;       // 四个刷新点
    public GameObject cookie;         // 饼干/体力点道具预制体
    public GameObject health;         // 血包预制体
    public PlayerHealth playerHealth; // 需要监控血量的玩家健康组件

   
    public float cookieRefreshInterval = 10f;

    private float cookieTimer = 0f;

    // 记录当前已生成的道具及其所在点
    private List<SpawnedItem> activeCookies = new List<SpawnedItem>();
    private List<SpawnedItem> activeHealths = new List<SpawnedItem>();

    // 点位占用情况，用于避免同一点同时出现多个道具
    private bool[] cookieOccupied;
    private bool[] healthOccupied;

    void Start()
    {
        if (point == null)
            point = new Transform[0];

        cookieOccupied = new bool[point.Length];
        healthOccupied = new bool[point.Length];
    }


    void Update()
    {
        // 清理已被销毁的道具，并同步占用状态
        PruneAndSyncOccupancy();

        // Cookie（饼干）刷新逻辑
        if (cookie != null && point.Length > 0 && activeCookies.Count < 4)
        {
            cookieTimer += Time.deltaTime;
            if (cookieTimer >= cookieRefreshInterval)
            {
                var availableIndices = GetAvailableIndices(cookieOccupied, healthOccupied);
                if (availableIndices.Count > 0)
                {
                    int idx = availableIndices[Random.Range(0, availableIndices.Count)];
                    var obj = Instantiate(cookie, point[idx].position, point[idx].rotation);
                    activeCookies.Add(new SpawnedItem { instance = obj, pointIndex = idx });
                    cookieOccupied[idx] = true; // 标记该点已被占用
                }
                cookieTimer = 0f;
            }
        }

        // Health（血包）刷新逻辑：仅在血量严格小于 50 时刷新，且同时存在的血包数量小于 2
        if (health != null && playerHealth != null && activeHealths.Count < 2
            && playerHealth.CurrentHealth > 0 && playerHealth.CurrentHealth < 50)
        {
            var availableIndices = GetAvailableIndices(cookieOccupied, healthOccupied);
            if (availableIndices.Count > 0)
            {
                int idx = availableIndices[Random.Range(0, availableIndices.Count)];
                var objH = Instantiate(health, point[idx].position, point[idx].rotation);
                activeHealths.Add(new SpawnedItem { instance = objH, pointIndex = idx });
                healthOccupied[idx] = true;
            }
        }
    }

    // 清理已销毁的道具，并重置占用标记
    private void PruneAndSyncOccupancy()
    {
        // 移除已消失的饼干
        activeCookies.RemoveAll(item => item.instance == null);

        // 移除已消失的血包
        activeHealths.RemoveAll(item => item.instance == null);

        // 重置占用状态
        for (int i = 0; i < cookieOccupied.Length; i++) cookieOccupied[i] = false;
        foreach (var c in activeCookies) if (c.instance != null) cookieOccupied[c.pointIndex] = true;

        for (int i = 0; i < healthOccupied.Length; i++) healthOccupied[i] = false;
        foreach (var h in activeHealths) if (h.instance != null) healthOccupied[h.pointIndex] = true;
    }

    // 获取未被占用的点的索引列表（排除了当前被 cookie 或 health 占用的点）
    private List<int> GetAvailableIndices(bool[] cookieOcc, bool[] healthOcc)
    {
        var list = new List<int>();
        for (int i = 0; i < point.Length; i++)
        {
            bool isOccupied = (i < cookieOcc.Length && cookieOcc[i]) || (i < healthOcc.Length && healthOcc[i]);
            if (!isOccupied)
            {
                list.Add(i);
            }
        }
        return list;
    }

    // 简单的结构体，记录实例对象及其所在点的索引
    private class SpawnedItem
    {
        public GameObject instance;
        public int pointIndex;
    }
}
