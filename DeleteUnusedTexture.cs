using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class UnusedTextureCleaner : EditorWindow
{
    [MenuItem("Tools/清理未使用贴图")]
    public static void ShowWindow()
    {
        GetWindow<UnusedTextureCleaner>("未使用贴图清理工具");
    }

    private Vector2 scrollPosition;
    private List<string> unusedTextures = new List<string>();
    private bool showConfirmation = false;

    void OnGUI()
    {
        GUILayout.Label("未使用贴图清理工具", EditorStyles.boldLabel);
        
        if (GUILayout.Button("查找未使用贴图"))
        {
            FindUnusedTextures();
        }

        if (unusedTextures.Count > 0)
        {
            GUILayout.Label($"找到 {unusedTextures.Count} 个未使用的贴图:");
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            foreach (var texturePath in unusedTextures)
            {
                EditorGUILayout.LabelField(texturePath);
            }
            EditorGUILayout.EndScrollView();

            if (!showConfirmation)
            {
                if (GUILayout.Button("删除未使用贴图"))
                {
                    showConfirmation = true;
                }
            }
            else
            {
                GUILayout.Label("确定要删除这些贴图吗？此操作不可撤销！");
                
                if (GUILayout.Button("确认删除"))
                {
                    DeleteUnusedTextures();
                    showConfirmation = false;
                }
                
                if (GUILayout.Button("取消"))
                {
                    showConfirmation = false;
                }
            }
        }
        else if (GUILayout.Button("已查找但未找到未使用贴图"))
        {
            EditorGUILayout.HelpBox("没有找到未使用的贴图，或者尚未执行查找操作。", MessageType.Info);
        }
    }

    void FindUnusedTextures()
    {
        unusedTextures.Clear();
        
        // 获取项目中所有贴图
        string[] allTextures = AssetDatabase.FindAssets("t:Texture", new[] {"Assets"});
        
        // 获取场景中使用的贴图
        HashSet<string> usedTextures = new HashSet<string>();
        
        // 检查场景中的材质
        Material[] allMaterials = Resources.FindObjectsOfTypeAll<Material>();
        foreach (Material mat in allMaterials)
        {
            if (mat != null && mat.shader != null)
            {
                int propertyCount = ShaderUtil.GetPropertyCount(mat.shader);
                for (int i = 0; i < propertyCount; i++)
                {
                    if (ShaderUtil.GetPropertyType(mat.shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                    {
                        string propertyName = ShaderUtil.GetPropertyName(mat.shader, i);
                        Texture texture = mat.GetTexture(propertyName);
                        if (texture != null)
                        {
                            string path = AssetDatabase.GetAssetPath(texture);
                            if (!string.IsNullOrEmpty(path))
                            {
                                usedTextures.Add(path);
                            }
                        }
                    }
                }
            }
        }
        
        // 检查其他可能使用贴图的地方（如UI Image等）
        UnityEngine.UI.Image[] allImages = Resources.FindObjectsOfTypeAll<UnityEngine.UI.Image>();
        foreach (var image in allImages)
        {
            if (image.sprite != null && image.sprite.texture != null)
            {
                string path = AssetDatabase.GetAssetPath(image.sprite.texture);
                if (!string.IsNullOrEmpty(path))
                {
                    usedTextures.Add(path);
                }
            }
        }
        
        // 找出未使用的贴图
        foreach (string textureGuid in allTextures)
        {
            string texturePath = AssetDatabase.GUIDToAssetPath(textureGuid);
            if (!usedTextures.Contains(texturePath))
            {
                unusedTextures.Add(texturePath);
            }
        }
        
        EditorUtility.DisplayDialog("完成", $"找到 {unusedTextures.Count} 个未使用的贴图", "确定");
    }

    void DeleteUnusedTextures()
    {
        int successCount = 0;
        int failCount = 0;
        
        foreach (string texturePath in unusedTextures)
        {
            if (AssetDatabase.DeleteAsset(texturePath))
            {
                successCount++;
            }
            else
            {
                failCount++;
                Debug.LogError($"无法删除贴图: {texturePath}");
            }
        }
        
        unusedTextures.Clear();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("完成", 
            $"已删除 {successCount} 个贴图\n{failCount} 个贴图删除失败", 
            "确定");
    }
}