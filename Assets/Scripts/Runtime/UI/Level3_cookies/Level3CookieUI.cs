using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class Level3CookieUI : MonoBehaviour
{
    public static Level3CookieUI Instance;

    [Header("References")]
    public RectTransform cookieContainer;   // UI 容器（建议就是你的 ShardContainer）
    public Sprite cookieSprite;             // 你的 cookieicon（Sprite 资源）
    public TMP_Text callLabel;              // 左上角 “Call” 文本（可选；若不需要可留空）

    [Header("Icon Layout")]
    public Vector2 iconSize = new Vector2(32, 32); // 每个图标尺寸（如容器有Layout可忽略）
    public bool useNativeSize = false;             // 勾上后用Sprite原图尺寸
    public bool clearWhenZero = false;             // 为 true 则当数量为0时清空/隐藏容器

    private readonly List<Image> _icons = new();

    void Awake()
    {
        Instance = this;
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
        int cur = CookiesInventory.Instance ? CookiesInventory.Instance.cookies : 0;
        EnsureIcons(cur);
        Refresh(cur);
    }

    /// <summary>
    /// 根据 current 数量，创建/复用足够的 Image 图标对象
    /// </summary>
    void EnsureIcons(int current)
    {
        if (!cookieContainer || !cookieSprite) return;

        // 如果当前需求比已有多，则创建缺少的
        for (int i = _icons.Count; i < current; i++)
        {
            var go = new GameObject($"cookie_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(cookieContainer, worldPositionStays: false);

            var img = go.GetComponent<Image>();
            img.sprite = cookieSprite;
            img.type = Image.Type.Simple;
            img.raycastTarget = false;

            if (useNativeSize) img.SetNativeSize();
            else rt.sizeDelta = iconSize;

            go.SetActive(true);
            _icons.Add(img);
        }
    }

    /// <summary>
    /// 外部/事件回调：当 Cookie 数量变化时更新 UI
    /// </summary>
    public void Refresh(int current)
    {
        current = Mathf.Max(0, current);

        // 先确保有足够的图标对象（只增不减，减少时用隐藏来避免GC抖动）
        EnsureIcons(current);

        // 按数量开关显示
        for (int i = 0; i < _icons.Count; i++)
            _icons[i].gameObject.SetActive(i < current);

        // 没有 Cookie 时的额外处理（可选）
        if (cookieContainer)
            cookieContainer.gameObject.SetActive(!clearWhenZero || current > 0);

        // 更新“Call (xN)”文本（如不需要，可不绑定 callLabel）
        if (callLabel)
            callLabel.text = $"Call ({current})";

        // 若容器上挂了 Horizontal/Vertical/Grid Layout Group，下面这句会强制刷新一次布局
        LayoutRebuilder.MarkLayoutForRebuild(cookieContainer);
    }
}
