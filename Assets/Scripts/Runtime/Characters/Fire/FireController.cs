using UnityEngine;

public class FireController : MonoBehaviour
{
    [Header("Fire Shrink Settings")]
    public float shrinkDuration = 2.0f;
    public AnimationCurve shrinkCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Fire Reset Settings")]
    public float resetDuration = 2.0f;
    public AnimationCurve resetCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Child Fire Settings")]
    public bool shrinkFromBottomCenter = true;

    private Transform[] fireChildren;
    private Vector3[] childInitialPositions;
    private Vector3[] childInitialScales;
    private Bounds[] childInitialBounds;

    private bool shouldShrink = false;
    private bool shouldReset = false;
    private float shrinkTimer = 0f;
    private float resetTimer = 0f;

    // Store the current state for use in restoring the animation
    private Vector3[] currentScales;
    private Vector3[] currentPositions;

    public bool IsShrinking { get; private set; } = false;
    public bool IsResetting { get; private set; } = false;
    public bool IsFullyShrunk { get; private set; } = false;
    public bool IsFullyReset { get; private set; } = false;

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
        currentScales = new Vector3[childCount];
        currentPositions = new Vector3[childCount];

        for (int i = 0; i < childCount; i++)
        {
            fireChildren[i] = transform.GetChild(i);
            childInitialPositions[i] = fireChildren[i].localPosition;
            childInitialScales[i] = fireChildren[i].localScale;
            currentScales[i] = childInitialScales[i];
            currentPositions[i] = childInitialPositions[i];

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
                        ApplyBottomCenterShrink(fireChildren[i], i, scaleFactor);
                    }
                    else
                    {
                        fireChildren[i].localScale = childInitialScales[i] * scaleFactor;
                    }
                    
                    // update
                    currentScales[i] = fireChildren[i].localScale;
                    currentPositions[i] = fireChildren[i].localPosition;
                }
            }

            if (progress >= 1.0f)
            {
                IsShrinking = false;
                IsFullyShrunk = true;
                SetChildrenActive(false);
            }
        }

        if (shouldReset && !IsFullyReset)
        {
            resetTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(resetTimer / resetDuration);
            
            for (int i = 0; i < fireChildren.Length; i++)
            {
                if (fireChildren[i] != null)
                {
                    if (shrinkFromBottomCenter)
                    {
                        // Interpolate from the current state to the initial state
                        fireChildren[i].localScale = Vector3.Lerp(currentScales[i], childInitialScales[i], progress);
                        fireChildren[i].localPosition = Vector3.Lerp(currentPositions[i], childInitialPositions[i], progress);
                    }
                    else
                    {
                        fireChildren[i].localScale = Vector3.Lerp(currentScales[i], childInitialScales[i], progress);
                    }
                }
            }

            if (progress >= 1.0f)
            {
                IsResetting = false;
                IsFullyReset = true;
                SetChildrenActive(true);
                
                // Ensure that the final state is completely correct.
                for (int i = 0; i < fireChildren.Length; i++)
                {
                    if (fireChildren[i] != null)
                    {
                        fireChildren[i].localScale = childInitialScales[i];
                        fireChildren[i].localPosition = childInitialPositions[i];
                    }
                }
            }
        }
    }

    void ApplyBottomCenterShrink(Transform child, int index, float scaleFactor)
    {
        Vector3 newScale = childInitialScales[index] * scaleFactor;
        child.localScale = newScale;

        if (scaleFactor > 0)
        {
            float heightDifference = childInitialScales[index].y - newScale.y;
            Vector3 newPosition = childInitialPositions[index];
            newPosition.y += heightDifference * 0.5f;
            child.localPosition = newPosition;
        }
    }

    public void StartShrink()
    {
        InitializeChildFires();
        shouldShrink = true;
        shouldReset = false;
        IsShrinking = true;
        IsResetting = false;
        IsFullyShrunk = false;
        IsFullyReset = false;
        shrinkTimer = 0f;
        resetTimer = 0f;
        SetChildrenActive(true);
    }

    public void StartReset()
    {
        // Save the current state before starting the restoration process.
        for (int i = 0; i < fireChildren.Length; i++)
        {
            if (fireChildren[i] != null)
            {
                currentScales[i] = fireChildren[i].localScale;
                currentPositions[i] = fireChildren[i].localPosition;
            }
        }
        
        shouldReset = true;
        shouldShrink = false;
        IsResetting = true;
        IsShrinking = false;
        IsFullyReset = false;
        IsFullyShrunk = false;
        resetTimer = 0f;
        shrinkTimer = 0f;
        SetChildrenActive(true);
    }

    public void InstantResetFire()
    {
        shouldShrink = false;
        shouldReset = false;
        IsShrinking = false;
        IsResetting = false;
        IsFullyShrunk = false;
        IsFullyReset = false;
        shrinkTimer = 0f;
        resetTimer = 0f;

        for (int i = 0; i < fireChildren.Length; i++)
        {
            if (fireChildren[i] != null)
            {
                fireChildren[i].localPosition = childInitialPositions[i];
                fireChildren[i].localScale = childInitialScales[i];
                currentScales[i] = childInitialScales[i];
                currentPositions[i] = childInitialPositions[i];
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