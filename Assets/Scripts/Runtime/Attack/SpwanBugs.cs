using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class SpwanBugs : MonoBehaviour
{
    public static SpwanBugs Instance; // 单例实例

    public GameObject bugPrefab;                 // 主虫子预制体
    public GameObject secondBugPrefab;           // 第二种虫子预制体
    public float bugPrefabChance = 0.5f;         // 出现第一种虫子的概率（0..1）

    public Transform[] spawnPoints;

    public float minSpeed = 1f;
    public float maxSpeed = 3f;

    [Range(0f, 1f)]
    public float cookieDisplayChance = 0.5f;

    public bool startOnAwake = true;
    
    public bool[] reverseDirection;              // 控制每个生成点的方向是否反转

    public int maxPrimaryBugs = 5;               // 最大第一种虫子数量
    private int currentPrimaryBugs = 0;          // 当前场上的第一种虫子数量

    // 在类中添加
    private List<List<GameObject>> spawnedBugsPerPoint = new List<List<GameObject>>();

    void Awake()
    {
        // 单例模式初始化
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        
        // 初始化maxPrimaryBugs为5
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

        // 初始化方向数组
        if (reverseDirection.Length != spawnPoints.Length)
        {
            Array.Resize(ref reverseDirection, spawnPoints.Length);
        }

        // 初始化列表
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

            //先随机生成一个速度，三只虫子共享这个速度
            float sharedSpeed = UnityEngine.Random.Range(minSpeed, maxSpeed);

           
            List<GameObject> currentBatch = new List<GameObject>();

            // 连续生成三只虫子，每只之间有0.5秒间隔
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

                // 根据虫子类型增加相应的计数
                if (chosenPrefab == bugPrefab)
                {
                    currentPrimaryBugs++;
                }
               
                currentBatch.Add(bug);

              
                float speed = sharedSpeed;

                // 确定移动方向（根据是否反转）
                Vector3 moveDir = spPoint.forward;
                if (reverseDirection[index])
                {
                    moveDir = -moveDir; // 反转方向
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

                // 添加销毁事件监听
                BugDestroyListener listener = bug.AddComponent<BugDestroyListener>();
                listener.onDestroyed = () => {
                    if (chosenPrefab == bugPrefab)
                    {
                        currentPrimaryBugs = Math.Max(0, currentPrimaryBugs - 1);
                    }
                };

                SetCookieOnBug(bug);

                // 每只虫子之间间隔0.5秒
                if (i < 2) // 前两只虫子生成后等待，最后一只不需要等待
                {
                    yield return new WaitForSeconds(0.5f);
                }
            }

            // 等待当前批次的所有虫子死亡
            while (currentBatch.Count > 0)
            {
                // 检查并移除已经死亡的虫子
                for (int i = currentBatch.Count - 1; i >= 0; i--)
                {
                    if (currentBatch[i] == null || !currentBatch[i].activeInHierarchy)
                    {
                        currentBatch.RemoveAt(i);
                    }
                }

                // 如果还有存活的虫子，继续等待
                if (currentBatch.Count > 0)
                {
                    yield return new WaitForSeconds(0.5f); // 每0.5秒检查一次
                }
            }

            //在下一批生成前添加一个短暂延迟让他没那么快生成 = =
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
                // 根据随机概率显示
                bool show = UnityEngine.Random.value < cookieDisplayChance;
                t.gameObject.SetActive(show);
                if (show)
                {
                    cookieObj = t.gameObject;
                    // 为 Cookie 添加跟随/掉落逻辑
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

    // 新增方法：减少maxPrimaryBugs计数
    public void DecreaseMaxPrimaryBugs()
    {

        maxPrimaryBugs = Math.Max(0, maxPrimaryBugs - 1);
        Debug.Log($"Max primary bugs decreased. New value: {maxPrimaryBugs}");
    }
}

// 辅助组件，用于监听虫子销毁事件
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