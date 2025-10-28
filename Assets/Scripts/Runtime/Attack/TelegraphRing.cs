using UnityEngine;

public class TelegraphRing : MonoBehaviour
{
    public float life = 0.8f;
    public float startScale = 0.7f;
    public float endScale = 1.2f;
    public float startAlpha = 0.05f;
    public float endAlpha = 0.35f;

    float t;
    SpriteRenderer sr;  // 如果你用的是 Quad+Sprite；若用 Mesh/材质，自行替换

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        transform.localScale = Vector3.one * startScale;
        SetAlpha(startAlpha);
    }

    void Update()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / life);

        float s = Mathf.Lerp(startScale, endScale, k);
        transform.localScale = Vector3.one * s;

        SetAlpha(Mathf.Lerp(startAlpha, endAlpha, k));

        if (t >= life) Destroy(gameObject);
    }

    void SetAlpha(float a)
    {
        if (!sr) return;
        var c = sr.color; c.a = a; sr.color = c;
    }
}
