using System.Collections.Generic;
using UnityEngine;

public class ItemRefresh : MonoBehaviour
{
    public Transform[] point;       
    public GameObject cookie;         
    public GameObject health;         
    public PlayerHealth playerHealth; 

   
    public float cookieRefreshInterval = 10f;

    private float cookieTimer = 0f;


    private List<SpawnedItem> activeCookies = new List<SpawnedItem>();
    private List<SpawnedItem> activeHealths = new List<SpawnedItem>();


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

        PruneAndSyncOccupancy();


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
                    cookieOccupied[idx] = true; 
                }
                cookieTimer = 0f;
            }
        }

        // Health item refresh logic: if health is below 50, refresh health items
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

    // Prune and sync occupancy states
    private void PruneAndSyncOccupancy()
    {
        // Remove null instances from active cookies
        activeCookies.RemoveAll(item => item.instance == null);

        // Remove null instances from active health items
        activeHealths.RemoveAll(item => item.instance == null);

        // ����ռ��״̬
        for (int i = 0; i < cookieOccupied.Length; i++) cookieOccupied[i] = false;
        foreach (var c in activeCookies) if (c.instance != null) cookieOccupied[c.pointIndex] = true;

        for (int i = 0; i < healthOccupied.Length; i++) healthOccupied[i] = false;
        foreach (var h in activeHealths) if (h.instance != null) healthOccupied[h.pointIndex] = true;
    }

    // Get a list of available spawn points that are not currently occupied by either cookie or health items
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

    // Class to represent a spawned item and its associated spawn point
    private class SpawnedItem
    {
        public GameObject instance;
        public int pointIndex;
    }
}
