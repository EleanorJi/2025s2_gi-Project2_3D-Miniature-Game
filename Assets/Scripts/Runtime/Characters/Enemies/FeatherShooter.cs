using UnityEngine;

public class FeatherShooter : MonoBehaviour
{

    public Transform[] firePoints;

    public GameObject featherPrefab;


    public string playerTag = "Player";
    public float lagSeconds = 0.5f;


    public float projectileSpeed = 12f;
    public float turnRateDeg = 180f;

    public float lifeTime = 1.0f;
    public int damage = 20;
    public bool keepStartHeight = true;


    public bool useAutoTimer = true;
    public float firstDelay = 0.5f;
    public float fireInterval = 3f;
    public bool enableTestHotkey = true;  // P 
    public bool fireOnStartOnce = false; 

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
            Debug.LogWarning("[FeatherShooter] firePoints empty");
            return;
        }
        if (!featherPrefab)
        {
            Debug.LogWarning("[FeatherShooter] featherPrefab not set");
            return;
        }


        var probe = featherPrefab.GetComponent<FeatherProjectile>();
        if (!probe)
        {
            Debug.LogError("[FeatherShooter] Generation failed: The Feather Prefab root object you dragged in does not have the FeatherProjectile component.");
            return;
        }

        // delete last wave
        if (_lastWaveRoot) Destroy(_lastWaveRoot);
        _lastWaveRoot = new GameObject("FeatherWave");

        //_lastWaveRoot.transform.SetParent(transform, false);



        _lastWaveRoot.transform.SetParent(null, false);  // Do not follow the pigeon. The position rotation remains unchanged.
        _lastWaveRoot.transform.position = transform.position; // In the position of a pigeon
        _lastWaveRoot.transform.rotation = Quaternion.identity; // Cancel rotation
        probe.gameObject.transform.localScale=Vector3.one*0.5f;//Maintain the size of the feathers


        foreach (var fp in firePoints)
        {
            if (!fp) continue;


            Vector3 target = _player
                ? (_rec ? _rec.GetPastPosition(lagSeconds) : _player.position)
                : fp.position + fp.forward;

            Vector3 dir = target - fp.position; dir.y = 0f;
            if (dir.sqrMagnitude < 1e-6f) dir = fp.forward;
            dir.Normalize();

            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);

            var go = Instantiate(featherPrefab, fp.position, rot, _lastWaveRoot.transform);


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


            // Debug.Log($"[FeatherShooter] Fire one from {fp.name} → dir {dir}");
        }
    }
}
