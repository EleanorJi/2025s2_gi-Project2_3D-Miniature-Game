using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DissolveSphere : MonoBehaviour
{
    private Material mat;
    private bool isActive = false;
    private float animStartTime;
    
    [Header("Animation Timing (Total ~3s)")]
    [Tooltip("Phase 1: Collapse to 0.4")]
    public float collapsePhase1Duration = 1.0f;
    
    [Tooltip("Phase 2: Continue collapse to 1.0 + dissolve to 1.0")]
    public float combinedPhaseDuration = 2.0f;
    
    [Tooltip("Trigger particle effect when dissolve reaches this value")]
    public float particleTriggerDissolve = 0.45f;
    
    [Header("Particle Effect")]
    [Tooltip("Particle system name to find in child objects")]
    public string particleSystemName = "Ember_Particles";
    
    private ParticleSystem emberParticles;
    private bool particleTriggered = false;

    void Start()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            mat = new Material(renderer.material);
            renderer.material = mat;
        }
        
        // Initialize parameters to 0
        if (mat != null)
        {
            mat.SetFloat("_DissolveAmount", 0);
            mat.SetFloat("_AnimationPrompt", 0);
        }
    }

    void FindParticleSystem()
    {
        if (emberParticles != null) return;
        
        // Method 1: Search in sibling and parent objects
        Transform root = transform.parent;
        if (root != null)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.Equals(particleSystemName, System.StringComparison.OrdinalIgnoreCase))
                {
                    emberParticles = child.GetComponent<ParticleSystem>();
                    if (emberParticles != null)
                    {
                        Debug.Log($"[DissolveSphere] Found particle system: {child.name} (path: {GetFullPath(child)})");
                        return;
                    }
                }
            }
        }
        
        // Method 2: Search in entire death model hierarchy
        Transform searchRoot = transform;
        while (searchRoot.parent != null && searchRoot.parent.name != "ladybug" && searchRoot.parent.name != "ladybugblack")
        {
            searchRoot = searchRoot.parent;
        }
        
        foreach (Transform child in searchRoot.GetComponentsInChildren<Transform>(true))
        {
            if (child.name.Equals(particleSystemName, System.StringComparison.OrdinalIgnoreCase))
            {
                emberParticles = child.GetComponent<ParticleSystem>();
                if (emberParticles != null)
                {
                    Debug.Log($"[DissolveSphere] Found particle system: {child.name} (path: {GetFullPath(child)})");
                    return;
                }
            }
        }
        
        Debug.LogWarning($"[DissolveSphere] Particle system '{particleSystemName}' not found in {gameObject.name}");
    }
    
    string GetFullPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }

    /// <summary>
    /// Start death animation (collapse + dissolve + particle)
    /// </summary>
    public void StartDissolve(float speed = 1.0f)
    {
        isActive = true;
        animStartTime = Time.time;
        particleTriggered = false;
        
        // Find particle system when starting animation (ensure death model is active)
        FindParticleSystem();
        
        // Speed parameter can be used for overall timing adjustment (optional)
        // Currently using fixed time segments, but can be adjusted as needed
    }

    public void StopDissolve()
    {
        isActive = false;
        if (mat != null)
        {
            mat.SetFloat("_DissolveAmount", 0);
            mat.SetFloat("_AnimationPrompt", 0);
        }
    }

    void Update()
    {
        if (!isActive || mat == null) return;
        
        float elapsed = Time.time - animStartTime;
        float totalDuration = collapsePhase1Duration + combinedPhaseDuration;
        
        float collapseValue = 0f;
        float dissolveValue = 0f;
        
        // === Phase 1: Collapse only (0 -> 0.4) ===
        if (elapsed < collapsePhase1Duration)
        {
            float t1 = elapsed / collapsePhase1Duration;
            collapseValue = Mathf.Lerp(0f, 0.4f, t1);
            dissolveValue = 0f;
        }
        // === Phase 2: Continue collapse (0.4 -> 1.0) + Start dissolve (0 -> 1.0) ===
        else if (elapsed < totalDuration)
        {
            float t2 = (elapsed - collapsePhase1Duration) / combinedPhaseDuration;
            collapseValue = Mathf.Lerp(0.4f, 1.0f, t2);
            dissolveValue = Mathf.Lerp(0f, 1.0f, t2);
            
            // Trigger particle effect when dissolve reaches specified value
            if (!particleTriggered && dissolveValue >= particleTriggerDissolve)
            {
                particleTriggered = true;
                TriggerParticleEffect();
            }
        }
        // === After completion, maintain final state ===
        else
        {
            collapseValue = 1.0f;
            dissolveValue = 1.0f;
            // Optionally stop animation here
            // isActive = false;
        }
        
        // Apply to material
        mat.SetFloat("_AnimationPrompt", collapseValue);
        mat.SetFloat("_DissolveAmount", dissolveValue);
    }
    
    void TriggerParticleEffect()
    {
        if (emberParticles != null)
        {
            Debug.Log($"[DissolveSphere] Playing particle effect: {emberParticles.name}");
            emberParticles.Play();
        }
        else
        {
            Debug.LogWarning("[DissolveSphere] Particle system not found, cannot play");
        }
    }
}