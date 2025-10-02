using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public bool isActivated = false;
    public bool canReactivate = false; // 是否可以重复激活（默认为false，只能激活一次）
    private ParticleSystem particles;
    private Light checkpointLight;
    
    void Start()
    {
        particles = GetComponentInChildren<ParticleSystem>();
        checkpointLight = GetComponentInChildren<Light>();
        
        // 初始状态
        SetActivationVisuals(false);
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ActivateCheckpoint();
        }
    }
    
    public void ActivateCheckpoint()
    {
        // 如果已经激活过且不能重复激活，则直接返回
        if (isActivated && !canReactivate)
        {
            return;
        }
        
        if (!isActivated)
        {
            // 第一次激活
            isActivated = true;
            SetActivationVisuals(true);
            
            // 通知存档点管理器这个点被激活（作为新的存档点）
            CheckpointManager.Instance.SetCheckpointActivated(this);
            
            Debug.Log("Checkpoint activated for the first time: " + gameObject.name);
        }
        else if (canReactivate)
        {
            // 允许重复激活的情况
            CheckpointManager.Instance.SetCheckpointActivated(this);
            Debug.Log("Checkpoint reactivated: " + gameObject.name);
        }
    }
    
    private void SetActivationVisuals(bool activated)
    {
        // 控制粒子效果
        if (particles != null)
        {
            if (activated)
                particles.Play();
            else
                particles.Stop();
        }
        
        // 控制灯光
        if (checkpointLight != null)
        {
            checkpointLight.color = activated ? Color.green : Color.gray;
        }
    }
    
    // 重置存档点状态（如果需要）
    public void ResetCheckpoint()
    {
        isActivated = false;
        SetActivationVisuals(false);
    }
    
    // 强制激活（忽略重复激活限制）
    public void ForceActivate()
    {
        isActivated = true;
        SetActivationVisuals(true);
        CheckpointManager.Instance.SetCheckpointActivated(this);
    }
}