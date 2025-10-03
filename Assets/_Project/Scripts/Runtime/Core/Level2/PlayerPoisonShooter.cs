using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(ParticleSystem))]
public class PlayerPoisonShooter : MonoBehaviour
{
    public ParticleSystem poisonPS;        // 拖当前这个PS
    [Header("Spray")]
    public float sprayRate = 220f;         // 每秒发射的粒子数（像水枪）
    public float coneAngle = 3f;           // 发散角（度）

    private ParticleSystem.EmissionModule _emission;
    private ParticleSystem.ShapeModule _shape;
    private bool _spraying;
    private readonly List<ParticleCollisionEvent> _events = new();

    void Awake()
    {
        if (!poisonPS) poisonPS = GetComponent<ParticleSystem>();
        _emission = poisonPS.emission;
        _shape = poisonPS.shape;

        _emission.rateOverTime = 0f;       // 由脚本控制
        _shape.angle = coneAngle;          // 运行时也能改
    }

    void Update()
    {
        // 开始喷
        if (Input.GetKeyDown(KeyCode.P)) StartSpray();
        // 松开停
        if (Input.GetKeyUp(KeyCode.P)) StopSpray();

        // 允许运行时调锥角
        if (_shape.angle != coneAngle) _shape.angle = coneAngle;
    }

    void StartSpray()
    {
        _spraying = true;
        _emission.rateOverTime = sprayRate;
        if (!poisonPS.isPlaying) poisonPS.Play();
    }

    void StopSpray()
    {
        _spraying = false;
        _emission.rateOverTime = 0f;
        poisonPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    // 命中回调：维持你之前的判定
    void OnParticleCollision(GameObject other)
    {
        int count = ParticlePhysicsExtensions.GetCollisionEvents(poisonPS, other, _events);
        var death = other.GetComponent<InsectDeath>() ?? other.GetComponentInParent<InsectDeath>();
        if (death != null) death.Kill();
        _events.Clear();
    }
}
