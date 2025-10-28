using UnityEngine;
using System.Collections;

/// <summary>
/// Attach to particle effect root object: after instantiation/activation, automatically wait for all
/// ParticleSystem to finish playing (including child nodes), then destroy self.
/// Doesn't rely on Stop Action config, more stable.
/// </summary>
public class AutoDestroyParticle : MonoBehaviour
{
    private void OnEnable()
    {
        StartCoroutine(WaitAndKill());
    }

    private IEnumerator WaitAndKill()
    {
        // collect all particle systems in self and child objects
        ParticleSystem[] systems = GetComponentsInChildren<ParticleSystem>(true);

        // some systems need to wait one frame to enter playing state
        yield return null;

        // if any system doesn't have Play On Awake checked, play them here (just to be safe)
        foreach (var ps in systems)
        {
            if (ps && !ps.isPlaying) ps.Play(true);
        }

        // wait until all systems are completely "dead"
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

        // all finished playing, destroy effect object
        Destroy(gameObject);
    }
}
