using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SkillChargeUI : MonoBehaviour
{
    public static SkillChargeUI Instance;

    [Header("Refs")]
    public GameObject hudRoot;          // HUD_Cookies
    public Image skillCircle;           // Left circle Image
    public TMP_Text skillLabel;         // Skill name inside the circle
    public Transform shardContainer;    // ShardContainer
    public GameObject cookieIconPrefab; // CookieIcon prefab (UI)

    [Header("Counts")]
    public int maxShards = 10;          // Max number of icons to show
    public int readyThreshold = 3;      // Light up when >= 3

    [Header("Visuals")]
    public Sprite circleOff;            // Off sprite (optional; color fallback)
    public Sprite circleOn;             // On sprite (optional)
    public Color offColor = new Color(1,1,1,0.5f);
    public Color onColor  = Color.white;
    public float popScale = 1.25f;      // Tiny pop when a new shard appears
    public float popTime  = 0.08f;

    private readonly List<GameObject> _icons = new();

    void Awake()
    {
        Instance = this;
        if (hudRoot) hudRoot.SetActive(false); // Keep HUD hidden until the first shard shows up
    }

    void OnEnable()
    {
        if (CookiesInventory.Instance != null)
            CookiesInventory.Instance.OnChanged += Refresh;
    }

    void OnDisable()
    {
        if (CookiesInventory.Instance != null)
            CookiesInventory.Instance.OnChanged -= Refresh;
    }

    void Start()
    {
        // Prebuild maxShards slots (hidden) so we don’t Instantiate at runtime
        EnsureIconsBuilt();
        int cur = CookiesInventory.Instance ? CookiesInventory.Instance.cookies : 0;
        Refresh(cur);
    }

    void EnsureIconsBuilt()
    {
        if (!cookieIconPrefab || !shardContainer) return;
        if (_icons.Count >= maxShards) return;

        for (int i = _icons.Count; i < maxShards; i++)
        {
            var go = Instantiate(cookieIconPrefab, shardContainer);
            go.transform.localScale = Vector3.one;
            go.SetActive(false);
            _icons.Add(go);
        }
    }

    // Public API: call this when the inventory changes
    public void Refresh(int current)
    {
        //Debug.Log($"[UI] Refresh, count={current}, hudRoot={(hudRoot?hudRoot.name:"NULL")}, shard={(shardContainer?shardContainer.name:"NULL")}, prefab={(cookieIconPrefab?cookieIconPrefab.name:"NULL")}");
        Debug.Log($"[UI] Refresh, count={current}, hud={(hudRoot?hudRoot.name:"NULL")}, shard={(shardContainer?shardContainer.name:"NULL")}, prefab={(cookieIconPrefab?cookieIconPrefab.name:"NULL")}");

        if (hudRoot) hudRoot.SetActive(current > 0); // Show HUD after we have at least 1 shard

        // Toggle the big circle on/off
        bool ready = current >= readyThreshold;
        if (skillCircle)
        {
            if (circleOn && circleOff) skillCircle.sprite = ready ? circleOn : circleOff;
            skillCircle.color = ready ? onColor : offColor;
        }

        // Show the right number of shard icons
        EnsureIconsBuilt();

        current = Mathf.Clamp(current, 0, maxShards);
        for (int i = 0; i < _icons.Count; i++)
        {
            bool on = i < current;
            _icons[i].SetActive(on);

            // Newly lit icon gets a tiny pop (optional vibe)
            if (on)
            {
                var t = _icons[i].transform;
                StopAllCoroutines();
                StartCoroutine(Pop(t));
            }
        }
    }

    System.Collections.IEnumerator Pop(Transform t)
    {
        Vector3 a = Vector3.one;
        Vector3 b = Vector3.one * popScale;
        float t0 = 0f;
        while (t0 < popTime)
        {
            t0 += Time.unscaledDeltaTime;
            float k = t0 / popTime;
            t.localScale = Vector3.Lerp(a, b, k);
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    // For a skill button to check if it's usable
    public bool IsReady()
    {
        return CookiesInventory.Instance && CookiesInventory.Instance.cookies >= readyThreshold;
    }

    // Spend shards when casting a skill & let UI auto-refresh via the event
    public bool TrySpendForSkill()
    {
        if (!CookiesInventory.Instance) return false;
        if (!CookiesInventory.Instance.Spend(readyThreshold)) return false;
        // After spending, Refresh is triggered by the inventory event
        return true;
    }
}
