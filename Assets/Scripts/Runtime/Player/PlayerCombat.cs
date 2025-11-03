using UnityEngine;
using UnityEngine.Audio;

public class PlayerCombat : MonoBehaviour
{
    [Header("Shoot")]
    public Transform firePoint;               // fire point
    public GameObject projectilePrefab;       // projectile prefab
    public float fireCooldown = 0.3f;
    public int playerDamage = 5;              // player projectile damage
    private float lastShotTime;

    [Header("Summon (1 cookie per minion)")]
    public GameObject minionPrefab;           // minion prefab (includes MinionAnchor + MinionShooter)
    public float summonSpread = 0.6f;         // radius around player for spawning
    public float minionLifetime = 20f;        // minion lifetime
    public float minionFireInterval = 0.7f;   // minion fire interval
    public int minionDamage = 1;              // minion projectile damage

    [Header("Boss")]
    public Transform boss;                    // Boss (can be left empty, will find by tag at runtime)
    public string bossTag = "Boss";

    #region Audio

    public AudioClip[] shootClips;
    [Range(0f, 1f)] public float shootVolume = 0.8f;

    [Tooltip("Summon success sound effect. Multiple clips can be added, one will be played randomly.")]
    public AudioClip[] summonClips;
    [Range(0f, 1f)] public float summonVolume = 0.9f;

    [Tooltip("Cookie insufficient prompt sound effect (optional).")]
    public AudioClip[] errorClips;
    [Range(0f, 1f)] public float errorVolume = 0.8f;


    public AudioMixerGroup outputMixerGroup;

    public bool enablePitchJitter = true;
    [Tooltip("0.95~1.05。")]
    public Vector2 pitchRange = new Vector2(0.97f, 1.03f);

    private AudioSource sfxSource;
    #endregion

    void Awake()
    {

        sfxSource = GetComponent<AudioSource>();
        if (!sfxSource) sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f; // 2D sound
        if (outputMixerGroup) sfxSource.outputAudioMixerGroup = outputMixerGroup;
    }

    void Update()
    {
        if (Input.GetMouseButton(0)) TryShoot();

        // use K key to summon one minion
        if (Input.GetKeyDown(KeyCode.K)) TrySummonOneMinion();
    }

    void TryShoot()
    {
        if (!projectilePrefab || !firePoint) return;
        if (Time.time - lastShotTime < fireCooldown) return;

        lastShotTime = Time.time;

        var go = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        var proj = go.GetComponent<PoisonProjectile>();
        if (proj != null)
        {
            proj.damage    = playerDamage; // player damage
            proj.targetTag = "";          // hit anything
            proj.logHits   = true;         // log hits for debugging
        }

        // Play shoot sound effect
        PlaySFX(shootClips, shootVolume);
    }

    void TrySummonOneMinion()
    {
        if (CookiesInventory.Instance == null || !CookiesInventory.Instance.Spend(1))
        {
            // not enough cookies
            NoCookieUI.ShowCenter("You need at least 1 cookie to summon a minion.");
            Debug.Log("[Summon] Not enough cookies (need 1).");

            // Play error sound effect (optional)
            PlaySFX(errorClips, errorVolume);
            return;
        }

        // 2) Find Boss reference
        EnsureBoss();
        if (!minionPrefab) return;

        // 3) Randomly generate a position around the player (non-overlapping)
        Vector2 rnd = Random.insideUnitCircle * summonSpread;
        Vector3 spawnPos = transform.position + new Vector3(rnd.x, 0f, rnd.y);

        var m = Instantiate(minionPrefab, spawnPos, Quaternion.identity);

        // 4) Bind Anchor to follow the player and stay on the ground
        var anchor = m.GetComponent<MinionAnchor>();
        if (anchor)
        {
            anchor.follow = transform;
            anchor.localOffset = new Vector3(rnd.x, 0f, rnd.y);
        }

        // 5) Configure shooting logic
        var shooter = m.GetComponent<MinionShooter>();
        if (shooter)
        {
            shooter.projectilePrefab = projectilePrefab;
            shooter.firePoint = FindFirePointIn(m.transform);
            shooter.fireEvery = minionFireInterval;
            shooter.minionDamage = minionDamage;
            shooter.targetTag = string.IsNullOrEmpty(bossTag) ? "Boss" : bossTag;
            shooter.lifeTime = minionLifetime;
        }

        // 6) Disable collisions between minions and the player, as well as between minions
        DisableCollisions(m);

        // Play summon sound effect
        PlaySFX(summonClips, summonVolume);
    }

    Transform FindFirePointIn(Transform root)
    {
        var t = root.Find("firePoint");
        if (t) return t;
        foreach (Transform c in root.GetComponentsInChildren<Transform>(true))
            if (c.name.ToLower().Contains("fire")) return c;
        return root;
    }

    void EnsureBoss()
    {
        if (boss) return;
        var go = GameObject.FindGameObjectWithTag(string.IsNullOrEmpty(bossTag) ? "Boss" : bossTag);
        if (go) boss = go.transform;
    }


    void DisableCollisions(GameObject newMinion)
    {
        var newCols = newMinion.GetComponentsInChildren<Collider>(includeInactive: true);
        var playerCol = GetComponent<Collider>();

        // not collide with player
        if (playerCol)
        {
            foreach (var c in newCols)
                if (c) Physics.IgnoreCollision(c, playerCol, true);
        }

        // not collide with existing minions
        var allMinions = FindObjectsOfType<MinionAnchor>();
        foreach (var other in allMinions)
        {
            if (!other || other.gameObject == newMinion) continue;

            var otherCols = other.GetComponentsInChildren<Collider>(includeInactive: true);
            foreach (var c1 in newCols)
            {
                foreach (var c2 in otherCols)
                {
                    if (c1 && c2)
                        Physics.IgnoreCollision(c1, c2, true);
                }
            }
        }
    }


    void PlaySFX(AudioClip[] clips, float volume)
    {
        if (sfxSource == null || clips == null || clips.Length == 0) return;

        int idx = (clips.Length == 1) ? 0 : Random.Range(0, clips.Length);
        var clip = clips[idx];
        if (!clip) return;

        float originalPitch = sfxSource.pitch;

        if (enablePitchJitter && pitchRange.x > 0f && pitchRange.y > 0f)
        {
            float minP = Mathf.Min(pitchRange.x, pitchRange.y);
            float maxP = Mathf.Max(pitchRange.x, pitchRange.y);
            sfxSource.pitch = Random.Range(minP, maxP);
        }

        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));


        if (enablePitchJitter) sfxSource.pitch = originalPitch;
    }
}
