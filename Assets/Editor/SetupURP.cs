#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[InitializeOnLoad]
public static class SetupURP
{
    static SetupURP()
    {
        EditorApplication.delayCall += EnsureURPAssigned;
    }

    [MenuItem("Tools/Setup Universal RP")]
    public static void EnsureURPAssigned()
    {
        var urpAsset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
        if (urpAsset != null)
        {
            if (GraphicsSettings.defaultRenderPipeline != urpAsset)
            {
                GraphicsSettings.defaultRenderPipeline = urpAsset;
                QualitySettings.renderPipeline = urpAsset;
                EditorUtility.SetDirty(urpAsset);
                AssetDatabase.SaveAssets();
                Debug.Log("[URP Setup] Universal Render Pipeline (PC_RPAsset) successfully assigned to GraphicsSettings and QualitySettings!");
            }
        }
        else
        {
            Debug.LogWarning("[URP Setup] PC_RPAsset.asset not found at Assets/Settings/PC_RPAsset.asset");
        }
    }
}
#endif
