using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SkillChargeUI : MonoBehaviour
{
    public static SkillChargeUI Instance;

    [Header("Refs")]
    public GameObject hudRoot;          // HUD_Cookies
    public Image skillCircle;           // 左侧圆圈 Image
    public TMP_Text skillLabel;         // 圆圈里的技能名
    public Transform shardContainer;    // ShardContainer
    public GameObject cookieIconPrefab; // CookieIcon prefab（UI）

    [Header("Counts")]
    public int maxShards = 10;          // 最多显示几个
    public int readyThreshold = 3;      // >=3 点亮

    [Header("Visuals")]
    public Sprite circleOff;            // 熄灭图（可空，用颜色替代）
    public Sprite circleOn;             // 点亮图（可空）
    public Color offColor = new Color(1,1,1,0.5f);
    public Color onColor  = Color.white;
    public float popScale = 1.25f;      // 新增时小弹一下
    public float popTime  = 0.08f;

    private readonly List<GameObject> _icons = new();

    void Awake()
    {
        Instance = this;
        if (hudRoot) hudRoot.SetActive(false); // 等第一枚出现再显示
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
        // 预先创建 maxShards 个格子（隐藏），避免运行时频繁 Instantiate
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

    // 外部调用：背包变化时刷新
    public void Refresh(int current)
    {
        //Debug.Log($"[UI] Refresh, count={current}, hudRoot={(hudRoot?hudRoot.name:"NULL")}, shard={(shardContainer?shardContainer.name:"NULL")}, prefab={(cookieIconPrefab?cookieIconPrefab.name:"NULL")}");
        Debug.Log($"[UI] Refresh, count={current}, hud={(hudRoot?hudRoot.name:"NULL")}, shard={(shardContainer?shardContainer.name:"NULL")}, prefab={(cookieIconPrefab?cookieIconPrefab.name:"NULL")}");

        if (hudRoot) hudRoot.SetActive(current > 0); // 第1枚后显示

        // 点亮/熄灭圆圈
        bool ready = current >= readyThreshold;
        if (skillCircle)
        {
            if (circleOn && circleOff) skillCircle.sprite = ready ? circleOn : circleOff;
            skillCircle.color = ready ? onColor : offColor;
        }

        // 碎屑图标显示个数
        EnsureIconsBuilt();

        current = Mathf.Clamp(current, 0, maxShards);
        for (int i = 0; i < _icons.Count; i++)
        {
            bool on = i < current;
            _icons[i].SetActive(on);

            // 刚点亮的做个小弹动（可选）
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

    // 供技能按钮检查是否可用
    public bool IsReady()
    {
        return CookiesInventory.Instance && CookiesInventory.Instance.cookies >= readyThreshold;
    }

    // 供技能释放时扣除 & 刷新
    public bool TrySpendForSkill()
    {
        if (!CookiesInventory.Instance) return false;
        if (!CookiesInventory.Instance.Spend(readyThreshold)) return false;
        // 扣除后 Refresh 会被事件自动触发
        return true;
    }
}
