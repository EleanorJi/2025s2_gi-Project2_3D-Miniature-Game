using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DissolveSphere : MonoBehaviour
{
    private Material mat;
    private bool isActive = false;
    private float animStartTime;
    
    [Header("Animation Timing (Total ~3s)")]
    [Tooltip("阶段1：坍塌到0.4的时间")]
    public float collapsePhase1Duration = 1.0f;
    
    [Tooltip("阶段2：坍塌继续到1.0 + 消散到1.0 的时间")]
    public float combinedPhaseDuration = 2.0f;
    
    [Tooltip("消散达到这个值时播放粒子效果")]
    public float particleTriggerDissolve = 0.45f;
    
    [Header("Particle Effect")]
    [Tooltip("粒子系统名称（在子物体中查找）")]
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
        
        // 初始化参数为0
        if (mat != null)
        {
            mat.SetFloat("_DissolveAmount", 0);
            mat.SetFloat("_AnimationPrompt", 0);
        }
    }

    void FindParticleSystem()
    {
        if (emberParticles != null) return; // 已找到，不重复查找
        
        // 方法1: 在当前物体的兄弟节点和父物体中查找
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
                        Debug.Log($"[DissolveSphere] 找到粒子系统: {child.name} (路径: {GetFullPath(child)})");
                        return;
                    }
                }
            }
        }
        
        // 方法2: 在整个死亡模型中查找（向上找到根，再向下搜索）
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
                    Debug.Log($"[DissolveSphere] 找到粒子系统: {child.name} (路径: {GetFullPath(child)})");
                    return;
                }
            }
        }
        
        Debug.LogWarning($"[DissolveSphere] 在 {gameObject.name} 中未找到名为 '{particleSystemName}' 的粒子系统");
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
    /// 开始死亡动画（坍塌+消散+粒子）
    /// </summary>
    public void StartDissolve(float speed = 1.0f)
    {
        isActive = true;
        animStartTime = Time.time;
        particleTriggered = false;
        
        // 在启动动画时查找粒子系统（确保此时死亡模型已激活）
        FindParticleSystem();
        
        // speed参数可以用来整体调速（可选）
        // 这里我们使用固定的时间段，但您可以根据需要调整
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
        
        // === 阶段1：只坍塌（0 -> 0.4） ===
        if (elapsed < collapsePhase1Duration)
        {
            float t1 = elapsed / collapsePhase1Duration;
            collapseValue = Mathf.Lerp(0f, 0.4f, t1);
            dissolveValue = 0f;
        }
        // === 阶段2：坍塌继续（0.4 -> 1.0）+ 消散开始（0 -> 1.0） ===
        else if (elapsed < totalDuration)
        {
            float t2 = (elapsed - collapsePhase1Duration) / combinedPhaseDuration;
            collapseValue = Mathf.Lerp(0.4f, 1.0f, t2);
            dissolveValue = Mathf.Lerp(0f, 1.0f, t2);
            
            // 当消散到达指定值时，播放粒子效果
            if (!particleTriggered && dissolveValue >= particleTriggerDissolve)
            {
                particleTriggered = true;
                TriggerParticleEffect();
            }
        }
        // === 完成后保持最终状态 ===
        else
        {
            collapseValue = 1.0f;
            dissolveValue = 1.0f;
            // 可以选择在这里停止动画
            // isActive = false;
        }
        
        // 应用到材质
        mat.SetFloat("_AnimationPrompt", collapseValue);
        mat.SetFloat("_DissolveAmount", dissolveValue);
    }
    
    void TriggerParticleEffect()
    {
        if (emberParticles != null)
        {
            Debug.Log($"[DissolveSphere] 播放粒子效果: {emberParticles.name}");
            emberParticles.Play();
        }
        else
        {
            Debug.LogWarning("[DissolveSphere] 粒子系统未找到，无法播放");
        }
    }
}