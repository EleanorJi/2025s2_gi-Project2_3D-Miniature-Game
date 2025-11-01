using UnityEngine;

public class FeatherShooter : MonoBehaviour
{
    [Header("Fire Points (三个发射点)")]
    public Transform[] firePoints;

    [Header("Projectile Prefab (根预制体)")]
    public GameObject featherPrefab;  

    [Header("Targeting")]
    public string playerTag = "Player";
    public float lagSeconds = 0.5f;

    [Header("Projectile Params")]
    public float projectileSpeed = 12f;
    public float turnRateDeg = 180f;
    [Tooltip("单根羽毛生存时间；")]
    public float lifeTime = 1.0f;
    public int damage = 20;
    public bool keepStartHeight = true;

    [Header("Trigger")]
    public bool useAutoTimer = true;
    public float firstDelay = 0.5f;
    public float fireInterval = 3f;
    public bool enableTestHotkey = true;  // P 键
    public bool fireOnStartOnce = false;  // 进场立即打一轮（调试用）

    GameObject _lastWaveRoot;
    Transform _player;
    PlayerPositionRecorder _rec;

    void Awake()
    {
        var p = GameObject.FindGameObjectWithTag(playerTag);
        if (p) { _player = p.transform; _rec = _player.GetComponent<PlayerPositionRecorder>(); }
    }

    void Start()
    {
        // 定时器更稳，不受 Update 里其它逻辑影响
        if (useAutoTimer)
            InvokeRepeating(nameof(AnimEvent_FlapFire), firstDelay, fireInterval);

        if (fireOnStartOnce)
            AnimEvent_FlapFire();
    }

    void Update()
    {
        if (enableTestHotkey && Input.GetKeyDown(KeyCode.P))
            AnimEvent_FlapFire();
    }

    [ContextMenu("Test Fire Now")]
    public void AnimEvent_FlapFire()
    {
        if (firePoints == null || firePoints.Length == 0)
        {
            Debug.LogWarning("[FeatherShooter] firePoints 为空。");
            return;
        }
        if (!featherPrefab)
        {
            Debug.LogWarning("[FeatherShooter] featherPrefab 未设置。");
            return;
        }

        // 自检：预制体上必须有 FeatherProjectile
        var probe = featherPrefab.GetComponent<FeatherProjectile>();
        if (!probe)
        {
            Debug.LogError("[FeatherShooter] 生成失败：拖入的 Feather Prefab 根物体上没有 FeatherProjectile 组件。");
            return;
        }

        // 清理上一波（避免场景里越积越多）
        if (_lastWaveRoot) Destroy(_lastWaveRoot);
        _lastWaveRoot = new GameObject("FeatherWave");

        //_lastWaveRoot.transform.SetParent(transform, false);


        //修改bug:鸽子朝向玩家时羽毛转向问题
        _lastWaveRoot.transform.SetParent(null, false);  // 不跟随鸽子，位置旋转保持不变
        _lastWaveRoot.transform.position = transform.position; // 放在鸽子位置
        _lastWaveRoot.transform.rotation = Quaternion.identity; // 取消旋转
        probe.gameObject.transform.localScale=Vector3.one*0.5f;//保持羽毛大小


        foreach (var fp in firePoints)
        {
            if (!fp) continue;

            // 取目标的“滞后位置”，只在水平面朝向
            Vector3 target = _player
                ? (_rec ? _rec.GetPastPosition(lagSeconds) : _player.position)
                : fp.position + fp.forward;

            Vector3 dir = target - fp.position; dir.y = 0f;
            if (dir.sqrMagnitude < 1e-6f) dir = fp.forward;
            dir.Normalize();

            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);

            var go = Instantiate(featherPrefab, fp.position, rot, _lastWaveRoot.transform);

            // 赋参数
            var proj = go.GetComponent<FeatherProjectile>();
            proj.playerTag       = playerTag;
            proj.lagSeconds      = lagSeconds;
            proj.speed           = projectileSpeed;
            proj.turnRateDeg     = turnRateDeg;
            proj.lifeTime        = lifeTime;
            proj.damage          = damage;
            proj.keepStartHeight = keepStartHeight;

            
            if (!proj.visual && go.transform.childCount > 0)
                proj.visual = go.transform.GetChild(0);

            // 防呆打印
            // Debug.Log($"[FeatherShooter] Fire one from {fp.name} → dir {dir}");
        }
    }
}
