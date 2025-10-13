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
            
            // Store initial bounds for potential future use
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
                        // Shrink from the center at the bottom.
                        ApplyBottomCenterShrink(fireChildren[i], i, scaleFactor);
                    }
                    else
                    {
                        // Normal reduction
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
        // Apply scaling
        Vector3 newScale = childInitialScales[index] * scaleFactor;
        child.localScale = newScale;

        if (scaleFactor > 0)
        {
            // Calculate the position offset to maintain the bottom fixed
            float heightDifference = childInitialScales[index].y - newScale.y;
            Vector3 newPosition = childInitialPositions[index];
            newPosition.y += heightDifference * 0.5f; // Move upwards by half of the height difference
            child.localPosition = newPosition;
        }
    }

    public void StartShrink()
    {
        InitializeChildFires(); // Reinitialize before each start
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