using UnityEngine;
using Antventure.UI;
using System.Collections;

public class Checkpoint : MonoBehaviour
{
    [Header("基础设置")]
    public bool isActivated = false;
    public bool canReactivate = false; // 是否可以重复激活
    
    [Header("提示文字设置")]
    [TextArea(3, 6)]
    [SerializeField] private string hintText = "请输入提示文字";
    [SerializeField] private float autoHideDelay = 5f; // 自动消失时间（秒）
    [SerializeField] private bool showHintOnActivation = true; // 是否在激活时显示提示
    
    [Header("视觉组件")]
    private ParticleSystem particles;
    private Light checkpointLight;
    
    private bool hasTriggeredHint = false;
    private Coroutine hideCoroutine;

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
            
            // 显示提示文字（如果是第一次触发或者允许重复显示）
            if (showHintOnActivation && (!hasTriggeredHint || canReactivate))
            {
                ShowCheckpointHint();
                hasTriggeredHint = true;
            }
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
            CheckpointManager.Instance?.SetCheckpointActivated(this);
            
            Debug.Log("Checkpoint activated for the first time: " + gameObject.name);
        }
        else if (canReactivate)
        {
            // 允许重复激活的情况
            CheckpointManager.Instance?.SetCheckpointActivated(this);
            Debug.Log("Checkpoint reactivated: " + gameObject.name);
        }
    }
    
    /// <summary>
    /// 显示检查点提示文字
    /// </summary>
    public void ShowCheckpointHint()
    {
        if (UIManager.Instance != null && !string.IsNullOrEmpty(hintText))
        {
            UIManager.Instance.ShowHintText(hintText);
            
            // 如果有正在进行的隐藏协程，先停止它
            if (hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
            }
            
            // 开始新的自动隐藏协程
            hideCoroutine = StartCoroutine(AutoHideAfterDelay());
            
            Debug.Log($"显示检查点提示: {gameObject.name}，{autoHideDelay}秒后自动消失");
        }
    }
    
    private IEnumerator AutoHideAfterDelay()
    {
        // 等待指定时间
        yield return new WaitForSeconds(autoHideDelay);
        
        // 隐藏提示文字
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideHintText();
            Debug.Log($"自动隐藏检查点提示: {gameObject.name}");
        }
        
        hideCoroutine = null;
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
        hasTriggeredHint = false; // 重置提示触发状态
    }
    
    // 强制激活（忽略重复激活限制）
    public void ForceActivate()
    {
        isActivated = true;
        SetActivationVisuals(true);
        CheckpointManager.Instance?.SetCheckpointActivated(this);
        
        // 强制显示提示
        ShowCheckpointHint();
    }
    
    // 手动立即隐藏提示
    public void HideHint()
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideHintText();
        }
    }
    
    // 设置自动隐藏时间（可以在运行时调整）
    public void SetAutoHideDelay(float delay)
    {
        autoHideDelay = delay;
    }
    
    // 设置提示文字（可以在运行时调整）
    public void SetHintText(string newHintText)
    {
        hintText = newHintText;
    }
    
    // 当物体被禁用时，确保隐藏提示
    private void OnDisable()
    {
        HideHint();
    }
}