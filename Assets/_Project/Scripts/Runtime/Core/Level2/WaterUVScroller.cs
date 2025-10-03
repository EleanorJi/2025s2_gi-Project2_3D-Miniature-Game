using UnityEngine;

public class WaterUVScroller : MonoBehaviour
{
    public Material mat;              // 拖 Mat_WaterSurface 进来
    public Vector2 baseSpeed = new Vector2(0.02f, 0.01f);
    public bool useMainTex = false;   // 给 Albedo 放了贴图，勾
    public bool useNormalMap = true;  // 给 Normal Map 放了贴图，勾

    Vector2 mainOffset, bumpOffset;

    void Update()
    {
        if (!mat) return;
        float t = Time.time;

        if (useMainTex)
        {
            mainOffset = baseSpeed * t;
            mat.SetTextureOffset("_MainTex", mainOffset);
        }
        if (useNormalMap)
        {
            // 法线可以稍快一点更明显
            bumpOffset = baseSpeed * 1.5f * t;
            mat.SetTextureOffset("_BumpMap", bumpOffset);
        }
    }
}
