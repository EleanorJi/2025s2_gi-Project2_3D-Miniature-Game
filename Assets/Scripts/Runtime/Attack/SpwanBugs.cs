using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class SpwanBugs : MonoBehaviour
{
    public GameObject bugPrefab;                 // 主虫子预制体
    public GameObject secondBugPrefab;           // 第二种虫子预制体
    public float secondBugChance = 0.5f;         // 出现第二种虫子的概率（0..1）

    public Transform[] spawnPoints;

    public float minSpeed = 1f;
    public float maxSpeed = 3f;

    [Range(0f, 1f)]
    public float cookieDisplayChance = 0.5f;

    public bool startOnAwake = true;


    // 在类中添加
    private List<List<GameObject>> spawnedBugsPerPoint = new List<List<GameObject>>();

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

        //int count = Mathf.Min(6, spawnPoints.Length);
        //for (int i = 0; i < count; i++)
        //{
        //    StartCoroutine(SpawnAtPoint(i));
        //}

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

    //IEnumerator SpawnAtPoint(int index)
    //{
    //    Transform spPoint = spawnPoints[index];
    //    while (true)
    //    {
    //        if (spPoint == null)
    //        {
    //            yield break;
    //        }


    //        GameObject chosenPrefab = bugPrefab;

    //        if (secondBugPrefab != null && UnityEngine.Random.value < secondBugChance)
    //        {
    //            chosenPrefab = secondBugPrefab;
    //        }

    //        if (chosenPrefab == null)
    //        {
    //            yield break;
    //        }

    //        Vector3 pos = spPoint.position;
    //        Quaternion rot = spPoint.rotation;
    //        GameObject bug = Instantiate(chosenPrefab, pos, rot);


    //        float speed = UnityEngine.Random.Range(minSpeed, maxSpeed);


    //        Vector3 moveDir = spPoint.forward;
    //        if (moveDir.sqrMagnitude < 0.0001f) moveDir = Vector3.forward;



    //        bug.transform.rotation = Quaternion.LookRotation(moveDir, Vector3.up);

    //        // 计算目标旋转：朝向移动方向
    //        Quaternion lookRot = Quaternion.LookRotation(moveDir, Vector3.up);
    //        // 如果当前是第二种预制体，额外在 Z 轴旋转 90 度
    //        if (chosenPrefab == secondBugPrefab)
    //        {
    //            lookRot *= Quaternion.Euler(0f, 0f, -90f);
    //        }

    //        // 应用最终旋转
    //        bug.transform.rotation = lookRot;

    //        Rigidbody rb = bug.GetComponent<Rigidbody>();
    //        if (rb != null)
    //        {
    //            rb.linearVelocity = moveDir * speed;
    //        }

    //        SetCookieOnBug(bug);


    //        while (bug != null && bug.activeInHierarchy)
    //        {
    //            yield return null;
    //        }

    //        // 下一只虫子会在死亡后刷新，循环继续
    //        yield return null;
    //    }
    //}


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
                GameObject chosenPrefab = bugPrefab;

                if (secondBugPrefab != null && UnityEngine.Random.value < secondBugChance)
                {
                    chosenPrefab = secondBugPrefab;
                }

                if (chosenPrefab == null)
                {
                    yield break;
                }

                Vector3 pos = spPoint.position;
                Quaternion rot = spPoint.rotation;
                GameObject bug = Instantiate(chosenPrefab, pos, rot);

               
                currentBatch.Add(bug);

              
                float speed = sharedSpeed;

                Vector3 moveDir = spPoint.forward;
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

}
