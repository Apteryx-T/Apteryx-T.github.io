using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class FindDuplicateTextureReferences : EditorWindow
{
    [MenuItem("Tools/查找重复引用的贴图")]
    public static void ShowWindow()
    {
        GetWindow<FindDuplicateTextureReferences>("重复贴图引用检查器");
    }

    private Dictionary<Texture, List<Material>> textureToMaterialsMap = new Dictionary<Texture, List<Material>>();
    private Vector2 scrollPosition;

    private void OnGUI()
    {
        GUILayout.Label("查找被多个材质引用的贴图", EditorStyles.boldLabel);

        if (GUILayout.Button("分析场景中的材质和贴图"))
        {
            AnalyzeScene();
        }

        if (GUILayout.Button("清除结果"))
        {
            textureToMaterialsMap.Clear();
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        // 只显示被多个材质引用的贴图
        var duplicateTextures = textureToMaterialsMap.Where(pair => pair.Value.Count > 1)
                                                    .OrderByDescending(pair => pair.Value.Count);

        foreach (var pair in duplicateTextures)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField(pair.Key, typeof(Texture), false, GUILayout.Width(150));
            GUILayout.Label($"被 {pair.Value.Count} 个材质引用", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel++;
            foreach (var material in pair.Value)
            {
                EditorGUILayout.ObjectField(material, typeof(Material), false);
            }
            EditorGUI.indentLevel--;
            
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();
    }

    private void AnalyzeScene()
    {
        textureToMaterialsMap.Clear();

        // 获取项目中所有材质
        var allMaterials = Resources.FindObjectsOfTypeAll<Material>();

        foreach (var material in allMaterials)
        {
            // 获取材质使用的所有贴图属性
            var texturePropertyNames = material.GetTexturePropertyNames();

            foreach (var propertyName in texturePropertyNames)
            {
                var texture = material.GetTexture(propertyName);
                if (texture == null) continue;

                if (!textureToMaterialsMap.ContainsKey(texture))
                {
                    textureToMaterialsMap[texture] = new List<Material>();
                }

                if (!textureToMaterialsMap[texture].Contains(material))
                {
                    textureToMaterialsMap[texture].Add(material);
                }
            }
        }

        Debug.Log($"分析完成，共找到 {textureToMaterialsMap.Count} 张贴图，其中 {textureToMaterialsMap.Count(pair => pair.Value.Count > 1)} 张被多个材质引用");
    }
}
