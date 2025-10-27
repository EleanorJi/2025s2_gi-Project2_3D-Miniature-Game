using UnityEngine;

public class SetMaterialOpaqueRuntime : MonoBehaviour
{
    [Header("配置选项")]
    public bool executeOnStart = true;
    public bool includeChildren = true;
    
    void Start()
    {
        if (executeOnStart)
        {
            SetToOpaque();
        }
    }
    
    public void SetToOpaque()
    {
        Renderer[] renderers = includeChildren ? 
            GetComponentsInChildren<Renderer>() : 
            GetComponents<Renderer>();
        
        int count = 0;

        foreach (Renderer r in renderers)
        {
            // 使用 materials 而不是 sharedMaterials
            Material[] materials = r.materials;
            bool materialsChanged = false;
            
            for (int i = 0; i < materials.Length; i++)
            {
                Material mat = materials[i];
                if (mat != null && mat.shader.name.Contains("Standard"))
                {
                    // 创建材质实例，避免修改原始资源
                    Material newMat = new Material(mat);
                    SetupOpaqueMaterial(newMat);
                    materials[i] = newMat;
                    materialsChanged = true;
                    count++;
                }
            }
            
            if (materialsChanged)
            {
                r.materials = materials;
            }
        }

        Debug.Log($"✅ 已将 {count} 个材质改为 Opaque。");
    }
    
    private void SetupOpaqueMaterial(Material mat)
    {
        mat.SetFloat("_Mode", 0); // 0 = Opaque
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        mat.SetInt("_ZWrite", 1);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.DisableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = -1;
    }
    
    // 在 Inspector 中右键点击组件可执行
    [ContextMenu("执行设置为不透明")]
    private void ExecuteManual()
    {
        SetToOpaque();
    }
}