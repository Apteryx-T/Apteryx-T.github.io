using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public class UnusedModelCleaner : EditorWindow
{
    [MenuItem("Tools/清理未使用模型")]
    public static void ShowWindow()
    {
        GetWindow<UnusedModelCleaner>("未使用模型清理工具");
    }

    private Vector2 scrollPosition;
    private List<string> unusedModels = new List<string>();
    private bool showConfirmation = false;
    private bool includePrefabs = true;
    private bool includeScriptReferences = true;

    void OnGUI()
    {
        GUILayout.Label("未使用模型清理工具", EditorStyles.boldLabel);

        EditorGUILayout.Space();
        includePrefabs = EditorGUILayout.Toggle("检查Prefab引用", includePrefabs);
        includeScriptReferences = EditorGUILayout.Toggle("检查脚本引用", includeScriptReferences);

        EditorGUILayout.Space();
        if (GUILayout.Button("查找未使用模型"))
        {
            FindUnusedModels();
        }

        if (unusedModels.Count > 0)
        {
            GUILayout.Label($"找到 {unusedModels.Count} 个未使用的模型:");
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            foreach (var modelPath in unusedModels)
            {
                EditorGUILayout.LabelField(modelPath);
            }
            EditorGUILayout.EndScrollView();

            if (!showConfirmation)
            {
                if (GUILayout.Button("删除未使用模型"))
                {
                    showConfirmation = true;
                }
            }
            else
            {
                GUILayout.Label("确定要删除这些模型吗？此操作不可撤销！");
                
                if (GUILayout.Button("确认删除"))
                {
                    DeleteUnusedModels();
                    showConfirmation = false;
                }
                
                if (GUILayout.Button("取消"))
                {
                    showConfirmation = false;
                }
            }
        }
        else if (GUILayout.Button("已查找但未找到未使用模型"))
        {
            EditorGUILayout.HelpBox("没有找到未使用的模型，或者尚未执行查找操作。", MessageType.Info);
        }
    }

    void FindUnusedModels()
    {
        unusedModels.Clear();
        
        // 获取项目中所有模型文件
        string[] modelExtensions = new[] { ".fbx", ".obj", ".dae", ".3ds", ".blend", ".max", ".mb", ".ma" };
        List<string> allModelPaths = new List<string>();
        
        foreach (string extension in modelExtensions)
        {
            string[] guids = AssetDatabase.FindAssets($"t:Model{extension}");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!allModelPaths.Contains(path))
                {
                    allModelPaths.Add(path);
                }
            }
        }

        // 获取场景中使用的模型
        HashSet<string> usedModels = new HashSet<string>();

        // 1. 检查场景中的MeshFilter和SkinnedMeshRenderer
        MeshFilter[] meshFilters = Resources.FindObjectsOfTypeAll<MeshFilter>();
        foreach (MeshFilter filter in meshFilters)
        {
            if (filter.sharedMesh != null)
            {
                string path = AssetDatabase.GetAssetPath(filter.sharedMesh);
                if (!string.IsNullOrEmpty(path))
                {
                    usedModels.Add(path);
                }
            }
        }

        SkinnedMeshRenderer[] skinnedRenderers = Resources.FindObjectsOfTypeAll<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer renderer in skinnedRenderers)
        {
            if (renderer.sharedMesh != null)
            {
                string path = AssetDatabase.GetAssetPath(renderer.sharedMesh);
                if (!string.IsNullOrEmpty(path))
                {
                    usedModels.Add(path);
                }
            }
        }

        // 2. 检查动画模型使用的Avatar
        Animator[] animators = Resources.FindObjectsOfTypeAll<Animator>();
        foreach (Animator animator in animators)
        {
            if (animator.avatar != null)
            {
                string path = AssetDatabase.GetAssetPath(animator.avatar);
                if (!string.IsNullOrEmpty(path))
                {
                    usedModels.Add(path);
                }
            }
        }

        // 3. 检查Prefab引用（如果启用）
        if (includePrefabs)
        {
            string[] allPrefabs = AssetDatabase.FindAssets("t:Prefab");
            foreach (string prefabGuid in allPrefabs)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                
                if (prefab != null)
                {
                    // 检查Prefab中的组件
                    MeshFilter[] prefabFilters = prefab.GetComponentsInChildren<MeshFilter>(true);
                    foreach (MeshFilter filter in prefabFilters)
                    {
                        if (filter.sharedMesh != null)
                        {
                            string path = AssetDatabase.GetAssetPath(filter.sharedMesh);
                            if (!string.IsNullOrEmpty(path))
                            {
                                usedModels.Add(path);
                            }
                        }
                    }

                    SkinnedMeshRenderer[] prefabSkinnedRenderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                    foreach (SkinnedMeshRenderer renderer in prefabSkinnedRenderers)
                    {
                        if (renderer.sharedMesh != null)
                        {
                            string path = AssetDatabase.GetAssetPath(renderer.sharedMesh);
                            if (!string.IsNullOrEmpty(path))
                            {
                                usedModels.Add(path);
                            }
                        }
                    }

                    Animator[] prefabAnimators = prefab.GetComponentsInChildren<Animator>(true);
                    foreach (Animator animator in prefabAnimators)
                    {
                        if (animator.avatar != null)
                        {
                            string path = AssetDatabase.GetAssetPath(animator.avatar);
                            if (!string.IsNullOrEmpty(path))
                            {
                                usedModels.Add(path);
                            }
                        }
                    }
                }
            }
        }

        // 4. 检查脚本引用（如果启用）
        if (includeScriptReferences)
        {
            string[] allScripts = AssetDatabase.FindAssets("t:Script");
            foreach (string scriptGuid in allScripts)
            {
                string scriptPath = AssetDatabase.GUIDToAssetPath(scriptGuid);
                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
                
                if (script != null)
                {
                    string scriptText = File.ReadAllText(scriptPath);
                    foreach (string modelPath in allModelPaths)
                    {
                        string modelName = Path.GetFileNameWithoutExtension(modelPath);
                        if (scriptText.Contains(modelName))
                        {
                            usedModels.Add(modelPath);
                        }
                    }
                }
            }
        }

        // 找出未使用的模型
        foreach (string modelPath in allModelPaths)
        {
            if (!usedModels.Contains(modelPath))
            {
                unusedModels.Add(modelPath);
            }
        }

        EditorUtility.DisplayDialog("完成", $"找到 {unusedModels.Count} 个未使用的模型", "确定");
    }

    void DeleteUnusedModels()
    {
        int successCount = 0;
        int failCount = 0;
        
        foreach (string modelPath in unusedModels)
        {
            // 删除模型文件
            if (AssetDatabase.DeleteAsset(modelPath))
            {
                successCount++;
            }
            else
            {
                failCount++;
                Debug.LogError($"无法删除模型: {modelPath}");
            }

            // 尝试删除对应的.meta文件
            string metaPath = modelPath + ".meta";
            if (File.Exists(metaPath))
            {
                File.Delete(metaPath);
            }
        }
        
        unusedModels.Clear();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("完成", 
            $"已删除 {successCount} 个模型文件\n{failCount} 个文件删除失败", 
            "确定");
    }
}