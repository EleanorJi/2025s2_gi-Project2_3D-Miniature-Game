using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicShaderSwapper : MonoBehaviour
{
    [Header("材质设置")]
    public Material[] targetMaterials; // 要替换的目标材质数组（拖入Inspector）

    public float dissolveSpeed = 1.0f; // 溶解动画速度

    private DissolveSphere[] dissolveSpheres;
    private float dissolveStartTime; // 动画开始时间
    private bool isDissolving = false; // 溶解状态

    // 当前物体的溶解时间基值
    private float baseTime;

    //void Start()
    //{
    //    // 获取所有子物体中的DissolveSphere脚本
    //    dissolveSpheres = GetComponentsInChildren<DissolveSphere>(true);

    //}

    //// 替换所有材质为预设材质并激活溶解效果
    //public void SwitchToTargetMaterials()
    //{
    //    if (targetMaterials.Length == 0)
    //    {
    //        Debug.LogError("未设置目标材质！");
    //        return;
    //    }

    //    Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
    //    foreach (Renderer renderer in renderers)
    //    {
    //        Material[] newMats = new Material[renderer.materials.Length];
    //        for (int i = 0; i < newMats.Length; i++)
    //        {
    //            // 实例化目标材质，确保每个渲染器有自己的材质副本
    //            newMats[i] = Instantiate(targetMaterials[i % targetMaterials.Length]);
    //        }
    //        renderer.materials = newMats;
    //    }
    //    RefreshDissolveComponents();

    //}

    //// 刷新溶解组件引用
    //void RefreshDissolveComponents()
    //{
    //    // 获取所有DissolveSphere组件（包括新增的）
    //    dissolveSpheres = GetComponentsInChildren<DissolveSphere>(true);

    //    // 启用所有溶解脚本
    //    foreach (DissolveSphere dissolveSphere in dissolveSpheres)
    //    {
    //        if (dissolveSphere != null)
    //        {
    //            dissolveSphere.enabled = true;

    //        }
    //    }
    //}



    void Start()
    {
        RefreshDissolveComponents();
        baseTime = transform.GetHashCode() % 1000 * 0.001f; // 基于物体ID的随机基值
    }

    // 替换所有材质为预设材质并激活溶解效果
    public void SwitchToTargetMaterials()
    {
        if (targetMaterials.Length == 0)
        {
            Debug.LogError("未设置目标材质！");
            return;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            Material[] newMats = new Material[renderer.materials.Length];
            for (int i = 0; i < newMats.Length; i++)
            {
                newMats[i] = new Material(targetMaterials[i % targetMaterials.Length]);
            }
            renderer.materials = newMats;
        }

        RefreshDissolveComponents();
        StartDissolving();
    }

    // 刷新溶解组件引用
    void RefreshDissolveComponents()
    {
        dissolveSpheres = GetComponentsInChildren<DissolveSphere>(true);
    }

    // 开始溶解动画
    public void StartDissolving()
    {
        isDissolving = true;
        dissolveStartTime = Time.time;

        foreach (DissolveSphere ds in dissolveSpheres)
        {
            ds.enabled = true;
            if (ds != null)
            {
                //ds.EnableDissolve(true);
            }
               
        }
    }

    //// 停止溶解动画
    //public void StopDissolving()
    //{
    //    isDissolving = false;

    //    foreach (DissolveSphere ds in dissolveSpheres)
    //    {
    //        if (ds != null) ds.EnableDissolve(false);
    //    }
    //}

    // 获取统一的溶解值（同一物体共享）
    public float GetDissolveValue(float componentOffset = 0f)
    {
        if (!isDissolving) return 0;

        // 计算基于物体统一时间的溶解值
        float elapsedTime = (Time.time - dissolveStartTime) * dissolveSpeed;
        float dissolveValue = Mathf.Sin(elapsedTime + baseTime + componentOffset) / 2 + 0.5f;

        return dissolveValue;
    }
}
