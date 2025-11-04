using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicShaderSwapper : MonoBehaviour
{
    [Header("target")]
    public Material[] targetMaterials;

    public float dissolveSpeed = 1.0f;

    private DissolveSphere[] dissolveSpheres;
    private float dissolveStartTime;
    private bool isDissolving = false;
    private float baseTime;

    void Start()
    {
        RefreshDissolveComponents();
        baseTime = transform.GetHashCode() % 1000 * 0.001f; 
    }

    public void SwitchToTargetMaterials()
    {
        if (targetMaterials.Length == 0)
        {
            Debug.LogError("error");
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

    void RefreshDissolveComponents()
    {
        dissolveSpheres = GetComponentsInChildren<DissolveSphere>(true);
    }

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

    public float GetDissolveValue(float componentOffset = 0f)
    {
        if (!isDissolving) return 0;

        float elapsedTime = (Time.time - dissolveStartTime) * dissolveSpeed;
        float dissolveValue = Mathf.Sin(elapsedTime + baseTime + componentOffset) / 2 + 0.5f;

        return dissolveValue;
    }
}
