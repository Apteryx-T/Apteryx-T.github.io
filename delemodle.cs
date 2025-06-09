using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class UnusedModelsCleanup : EditorWindow
{
    [MenuItem("Tools/清理未使用的模型")]
    public static void ShowWindow()
    {
        GetWindow<UnusedModelsCleanup>("清理未使用的模型");
    }

    private void OnGUI()
    {
        GUILayout.Label("清理场景中未使用的模型", EditorStyles.boldLabel);

        if (GUILayout.Button("查找未使用的模型"))
        {
            FindUnusedModels();
        }

        if (GUILayout.Button("删除未使用的模型"))
        {
            DeleteUnusedModels();
        }
    }

    private void FindUnusedModels()
    {
        // 获取项目中所有模型
        string[] allModelPaths = AssetDatabase.FindAssets("t:Model")
            .Select(AssetDatabase.GUIDToAssetPath)
            .ToArray();

        // 获取场景中所有游戏对象
        GameObject[] allGameObjects = GameObject.FindObjectsOfType<GameObject>();

        // 收集所有被引用的模型路径
        HashSet<string> usedModelPaths = new HashSet<string>();

        foreach (GameObject go in allGameObjects)
        {
            // 检查MeshFilter组件
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                string path = AssetDatabase.GetAssetPath(meshFilter.sharedMesh);
                if (!string.IsNullOrEmpty(path))
                {
                    usedModelPaths.Add(path);
                }
            }

            // 检查SkinnedMeshRenderer组件
            SkinnedMeshRenderer skinnedMesh = go.GetComponent<SkinnedMeshRenderer>();
            if (skinnedMesh != null && skinnedMesh.sharedMesh != null)
            {
                string path = AssetDatabase.GetAssetPath(skinnedMesh.sharedMesh);
                if (!string.IsNullOrEmpty(path))
                {
                    usedModelPaths.Add(path);
                }
            }
        }

        // 找出未使用的模型
        List<string> unusedModelPaths = new List<string>();
        foreach (string modelPath in allModelPaths)
        {
            if (!usedModelPaths.Contains(modelPath))
            {
                unusedModelPaths.Add(modelPath);
            }
        }

        // 显示结果
        if (unusedModelPaths.Count > 0)
        {
            Debug.Log("找到 " + unusedModelPaths.Count + " 个未使用的模型:");
            foreach (string path in unusedModelPaths)
            {
                Debug.Log(path);
            }
        }
        else
        {
            Debug.Log("未找到未使用的模型");
        }
    }

    private void DeleteUnusedModels()
    {
        // 获取项目中所有模型
        string[] allModelPaths = AssetDatabase.FindAssets("t:Model")
            .Select(AssetDatabase.GUIDToAssetPath)
            .ToArray();

        // 获取场景中所有游戏对象
        GameObject[] allGameObjects = GameObject.FindObjectsOfType<GameObject>();

        // 收集所有被引用的模型路径
        HashSet<string> usedModelPaths = new HashSet<string>();

        foreach (GameObject go in allGameObjects)
        {
            // 检查MeshFilter组件
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                string path = AssetDatabase.GetAssetPath(meshFilter.sharedMesh);
                if (!string.IsNullOrEmpty(path))
                {
                    usedModelPaths.Add(path);
                }
            }

            // 检查SkinnedMeshRenderer组件
            SkinnedMeshRenderer skinnedMesh = go.GetComponent<SkinnedMeshRenderer>();
            if (skinnedMesh != null && skinnedMesh.sharedMesh != null)
            {
                string path = AssetDatabase.GetAssetPath(skinnedMesh.sharedMesh);
                if (!string.IsNullOrEmpty(path))
                {
                    usedModelPaths.Add(path);
                }
            }
        }

        // 找出未使用的模型
        List<string> unusedModelPaths = new List<string>();
        foreach (string modelPath in allModelPaths)
        {
            if (!usedModelPaths.Contains(modelPath))
            {
                unusedModelPaths.Add(modelPath);
            }
        }

        // 删除未使用的模型
        if (unusedModelPaths.Count > 0)
        {
            bool confirm = EditorUtility.DisplayDialog("确认删除",
                $"确定要删除 {unusedModelPaths.Count} 个未使用的模型吗？此操作不可撤销！",
                "删除", "取消");

            if (confirm)
            {
                foreach (string path in unusedModelPaths)
                {
                    AssetDatabase.DeleteAsset(path);
                    Debug.Log("已删除未使用的模型: " + path);
                }
                AssetDatabase.Refresh();
                Debug.Log($"已删除 {unusedModelPaths.Count} 个未使用的模型");
            }
        }
        else
        {
            Debug.Log("没有未使用的模型可删除");
        }
    }
}