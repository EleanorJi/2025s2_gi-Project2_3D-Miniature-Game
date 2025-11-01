using UnityEngine;
using System.Collections.Generic;

public class PlayerPositionRecorder : MonoBehaviour
{
    public float maxLookbackSeconds = 3f;

    struct Sample { public float t; public Vector3 p; }
    readonly List<Sample> _samples = new();

    void LateUpdate()
    {
        float now = Time.time;
        _samples.Add(new Sample { t = now, p = transform.position });

        float cutoff = now - maxLookbackSeconds;
        int i = 0;
        while (i < _samples.Count && _samples[i].t < cutoff) i++;
        if (i > 0) _samples.RemoveRange(0, i);
    }

    public Vector3 GetPastPosition(float secondsAgo)
    {
        if (_samples.Count == 0) return transform.position;
        float targetT = Time.time - Mathf.Max(0f, secondsAgo);

        for (int i = _samples.Count - 1; i >= 0; i--)
        {
            if (_samples[i].t <= targetT)
            {
                if (i < _samples.Count - 1)
                {
                    var a = _samples[i];
                    var b = _samples[i + 1];
                    float k = Mathf.InverseLerp(a.t, b.t, targetT);
                    return Vector3.Lerp(a.p, b.p, k);
                }
                return _samples[i].p;
            }
        }
        return _samples[0].p;
    }
}
