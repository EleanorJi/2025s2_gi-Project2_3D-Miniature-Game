using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

public class SetMaterialOpaque : EditorWindow
{
    [MenuItem("Tools/Set Selected Object Materials to Opaque")]
    public static void SetSelectedToOpaque()
    {
        GameObject obj = Selection.activeGameObject;
        if (obj == null)
        {
            Debug.LogWarning("No object is selected. Please select your model first.");
            return;
        }

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        int count = 0;

        foreach (Renderer r in renderers)
        {
            foreach (Material mat in r.sharedMaterials)
            {
                if (mat != null && mat.shader.name.Contains("Standard"))
                {
                    mat.SetFloat("_Mode", 0); // 0 = Opaque
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    mat.SetInt("_ZWrite", 1);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.DisableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = -1;

                    EditorUtility.SetDirty(mat);
                    count++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"{count} materials have been changed to Opaque.");
    }
}
#endif