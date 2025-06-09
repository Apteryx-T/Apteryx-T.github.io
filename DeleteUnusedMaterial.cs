using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class UnusedMaterialCleaner : EditorWindow
{
    [MenuItem("Tools/清理未使用材质")]
    public static void ShowWindow()
    {
        GetWindow<UnusedMaterialCleaner>("未使用材质清理工具");
    }

    private Vector2 scrollPosition;
    private List<Material> unusedMaterials = new List<Material>();
    private bool showConfirmation = false;

    void OnGUI()
    {
        GUILayout.Label("未使用材质清理工具", EditorStyles.boldLabel);

        if (GUILayout.Button("查找未使用材质"))
        {
            FindUnusedMaterials();
        }

        if (unusedMaterials.Count > 0)
        {
            GUILayout.Label($"找到 {unusedMaterials.Count} 个未使用的材质:");

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            foreach (var material in unusedMaterials)
            {
                EditorGUILayout.ObjectField(material, typeof(Material), false);
            }
            EditorGUILayout.EndScrollView();

            if (!showConfirmation)
            {
                if (GUILayout.Button("删除未使用材质"))
                {
                    showConfirmation = true;
                }
            }
            else
            {
                GUILayout.Label("确定要删除这些材质吗？此操作不可撤销！");

                if (GUILayout.Button("确认删除"))
                {
                    DeleteUnusedMaterials();
                    showConfirmation = false;
                }

                if (GUILayout.Button("取消"))
                {
                    showConfirmation = false;
                }
            }
        }
        else if (GUILayout.Button("已查找但未找到未使用材质"))
        {
            EditorGUILayout.HelpBox("没有找到未使用的材质，或者尚未执行查找操作。", MessageType.Info);
        }
    }

    void FindUnusedMaterials()
    {
        unusedMaterials.Clear();

        // 获取项目中所有材质
        string[] allMaterialGUIDs = AssetDatabase.FindAssets("t:Material");
        List<Material> allMaterials = new List<Material>();
        foreach (string guid in allMaterialGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null)
            {
                allMaterials.Add(mat);
            }
        }

        // 获取场景中使用的材质
        HashSet<Material> usedMaterials = new HashSet<Material>();

        // 1. 检查场景中的Renderer组件
        Renderer[] allRenderers = Resources.FindObjectsOfTypeAll<Renderer>();
        foreach (Renderer renderer in allRenderers)
        {
            if (renderer.sharedMaterials != null)
            {
                foreach (Material mat in renderer.sharedMaterials)
                {
                    if (mat != null)
                    {
                        usedMaterials.Add(mat);
                    }
                }
            }
        }

        // 2. 检查粒子系统
        ParticleSystemRenderer[] allParticleRenderers = Resources.FindObjectsOfTypeAll<ParticleSystemRenderer>();
        foreach (ParticleSystemRenderer renderer in allParticleRenderers)
        {
            if (renderer.sharedMaterials != null)
            {
                foreach (Material mat in renderer.sharedMaterials)
                {
                    if (mat != null)
                    {
                        usedMaterials.Add(mat);
                    }
                }
            }
        }

        // 3. 检查UI组件
        UnityEngine.UI.Graphic[] allGraphics = Resources.FindObjectsOfTypeAll<UnityEngine.UI.Graphic>();
        foreach (var graphic in allGraphics)
        {
            if (graphic.material != null)
            {
                usedMaterials.Add(graphic.material);
            }
        }

        // 4. 检查地形材质
        Terrain[] allTerrains = Resources.FindObjectsOfTypeAll<Terrain>();
        foreach (var terrain in allTerrains)
        {
            if (terrain.materialTemplate != null)
            {
                usedMaterials.Add(terrain.materialTemplate);
            }
        }

        // 找出未使用的材质
        foreach (Material mat in allMaterials)
        {
            if (!usedMaterials.Contains(mat))
            {
                unusedMaterials.Add(mat);
            }
        }

        EditorUtility.DisplayDialog("完成", $"找到 {unusedMaterials.Count} 个未使用的材质", "确定");
    }

    void DeleteUnusedMaterials()
    {
        int successCount = 0;
        int failCount = 0;

        foreach (Material material in unusedMaterials)
        {
            string path = AssetDatabase.GetAssetPath(material);
            if (!string.IsNullOrEmpty(path))
            {
                if (AssetDatabase.DeleteAsset(path))
                {
                    successCount++;
                }
                else
                {
                    failCount++;
                    Debug.LogError($"无法删除材质: {path}");
                }
            }
        }

        unusedMaterials.Clear();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("完成",
            $"已删除 {successCount} 个材质\n{failCount} 个材质删除失败",
            "确定");
    }
}