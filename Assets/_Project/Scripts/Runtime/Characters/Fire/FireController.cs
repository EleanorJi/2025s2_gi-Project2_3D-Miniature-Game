using UnityEngine;

public class FireController : MonoBehaviour
{
    [Header("Fire Shrink Settings")]
    public float shrinkDuration = 2.0f;
    public AnimationCurve shrinkCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Child Fire Settings")]
    public bool shrinkFromBottomCenter = true;

    private Transform[] fireChildren;
    private Vector3[] childInitialPositions;
    private Vector3[] childInitialScales;
    private Bounds[] childInitialBounds;

    private bool shouldShrink = false;
    private float shrinkTimer = 0f;

    public bool IsShrinking { get; private set; } = false;
    public bool IsFullyShrunk { get; private set; } = false;

    void Start()
    {
        InitializeChildFires();
    }

    void InitializeChildFires()
    {
        int childCount = transform.childCount;
        fireChildren = new Transform[childCount];
        childInitialPositions = new Vector3[childCount];
        childInitialScales = new Vector3[childCount];
        childInitialBounds = new Bounds[childCount];

        for (int i = 0; i < childCount; i++)
        {
            fireChildren[i] = transform.GetChild(i);
            childInitialPositions[i] = fireChildren[i].localPosition;
            childInitialScales[i] = fireChildren[i].localScale;
            
            // 计算每个子火焰的边界
            Renderer renderer = fireChildren[i].GetComponent<Renderer>();
            if (renderer != null)
            {
                childInitialBounds[i] = renderer.bounds;
            }
        }
    }

    void Update()
    {
        if (shouldShrink && !IsFullyShrunk)
        {
            shrinkTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(shrinkTimer / shrinkDuration);
            float scaleFactor = shrinkCurve.Evaluate(progress);
            
            for (int i = 0; i < fireChildren.Length; i++)
            {
                if (fireChildren[i] != null)
                {
                    if (shrinkFromBottomCenter)
                    {
                        // 从底部中心缩小
                        ApplyBottomCenterShrink(fireChildren[i], i, scaleFactor);
                    }
                    else
                    {
                        // 普通缩小
                        fireChildren[i].localScale = childInitialScales[i] * scaleFactor;
                    }
                }
            }
            
            if (progress >= 1.0f)
            {
                IsShrinking = false;
                IsFullyShrunk = true;
                SetChildrenActive(false);
            }
        }
    }

    void ApplyBottomCenterShrink(Transform child, int index, float scaleFactor)
    {
        // 应用缩放
        Vector3 newScale = childInitialScales[index] * scaleFactor;
        child.localScale = newScale;

        if (scaleFactor > 0)
        {
            // 计算位置偏移以保持底部固定
            float heightDifference = childInitialScales[index].y - newScale.y;
            Vector3 newPosition = childInitialPositions[index];
            newPosition.y += heightDifference * 0.5f; // 向上移动一半的高度差
            child.localPosition = newPosition;
        }
    }

    public void StartShrink()
    {
        InitializeChildFires(); // 每次开始前重新初始化
        shouldShrink = true;
        IsShrinking = true;
        IsFullyShrunk = false;
        shrinkTimer = 0f;
        SetChildrenActive(true);
    }

    public void ResetFire()
    {
        shouldShrink = false;
        IsShrinking = false;
        IsFullyShrunk = false;
        shrinkTimer = 0f;
        
        for (int i = 0; i < fireChildren.Length; i++)
        {
            if (fireChildren[i] != null)
            {
                fireChildren[i].localPosition = childInitialPositions[i];
                fireChildren[i].localScale = childInitialScales[i];
            }
        }
        
        SetChildrenActive(true);
    }

    private void SetChildrenActive(bool active)
    {
        foreach (Transform child in fireChildren)
        {
            if (child != null)
            {
                child.gameObject.SetActive(active);
            }
        }
    }
}