using UnityEngine;

public class WaterUVScroller : MonoBehaviour
{
    public Material mat;
    public Vector2 baseSpeed = new Vector2(0.02f, 0.01f);
    public bool useMainTex = false;
    public bool useNormalMap = true;

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
            
            bumpOffset = baseSpeed * 1.5f * t;
            mat.SetTextureOffset("_BumpMap", bumpOffset);
        }
    }
}
