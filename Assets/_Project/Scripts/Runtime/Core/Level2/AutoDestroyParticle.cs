using UnityEngine;
using System.Collections;

/// <summary>
/// 挂在粒子特效根物体上：实例化/激活后，自动等待所有
/// ParticleSystem 播放完毕（含子节点），然后销毁自身。
/// 不依赖 Stop Action 配置，更稳。
/// </summary>
public class AutoDestroyParticle : MonoBehaviour
{
    private void OnEnable()
    {
        StartCoroutine(WaitAndKill());
    }

    private IEnumerator WaitAndKill()
    {
        // 收集自身及子物体里的所有粒子系统
        ParticleSystem[] systems = GetComponentsInChildren<ParticleSystem>(true);

        // 有些系统需要等一帧才进入播放状态
        yield return null;

        // 如果有系统没有勾 Play On Awake，这里统一播放一下（安全起见）
        foreach (var ps in systems)
        {
            if (ps && !ps.isPlaying) ps.Play(true);
        }

        // 等到所有系统都完全“死亡”为止
        bool anyAlive;
        do
        {
            anyAlive = false;
            foreach (var ps in systems)
            {
                if (ps && ps.IsAlive(true)) { anyAlive = true; break; }
            }
            yield return null;
        }
        while (anyAlive);

        // 全部播放完，销毁特效对象
        Destroy(gameObject);
    }
}
