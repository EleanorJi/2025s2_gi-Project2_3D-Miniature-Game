using UnityEngine;

public class SetMaterialOpaqueRuntime : MonoBehaviour
{
    [Header("settings")]
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
            // use materials
            Material[] materials = r.materials;
            bool materialsChanged = false;
            
            for (int i = 0; i < materials.Length; i++)
            {
                Material mat = materials[i];
                if (mat != null && mat.shader.name.Contains("Standard"))
                {
                    // Create material instances to avoid modifying the original resources
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

        Debug.Log($"{count} materials have been changed to Opaque.");
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
    
    // Right-clicking on a component in Inspector will execute
    [ContextMenu("Set the execution to be opaque")]
    private void ExecuteManual()
    {
        SetToOpaque();
    }
}