using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class Level3CookieUI : MonoBehaviour
{
    public static Level3CookieUI Instance;

    [Header("References")]
    public RectTransform cookieContainer;   // UI container for cookie icons
    public Sprite cookieSprite;             // cookie icon (Sprite asset)
    public TMP_Text callLabel;              // Top-left "Call" text (optional; leave empty if not needed)

    [Header("Icon Layout")]
    public Vector2 iconSize = new Vector2(32, 32); // Size of each icon (ignore if container has Layout)
    public bool useNativeSize = false;             // If checked, use Sprite's native size
    public bool clearWhenZero = false;             // If true, clear/hide container when count is 0

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


    void EnsureIcons(int current)
    {
        if (!cookieContainer || !cookieSprite) return;

        // If current demand is greater than existing, create missing ones
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


    public void Refresh(int current)
    {
        current = Mathf.Max(0, current);

        // Ensure there are enough icon objects (only increase, not decrease; hide when reducing to avoid GC jitter)
        EnsureIcons(current);

        // Show/hide based on count
        for (int i = 0; i < _icons.Count; i++)
            _icons[i].gameObject.SetActive(i < current);

        // Additional handling when there are no Cookies (optional)
        if (cookieContainer)
            cookieContainer.gameObject.SetActive(!clearWhenZero || current > 0);

        // Update "Call (xN)" text (leave unbound if not needed)
        if (callLabel)
            callLabel.text = $"Call ({current})";

        // If the container has a Horizontal/Vertical/Grid Layout Group, the following line will force a layout rebuild
        LayoutRebuilder.MarkLayoutForRebuild(cookieContainer);
    }
}
