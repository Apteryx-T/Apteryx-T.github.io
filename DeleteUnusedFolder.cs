using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class UnusedFolderCleaner : EditorWindow
{
    [MenuItem("Tools/清理空文件夹")]
    public static void ShowWindow()
    {
        GetWindow<UnusedFolderCleaner>("空文件夹清理工具");
    }

    private Vector2 scrollPosition;
    private List<string> emptyFolders = new List<string>();
    private bool showConfirmation = false;

    void OnGUI()
    {
        GUILayout.Label("空文件夹清理工具", EditorStyles.boldLabel);
        
        if (GUILayout.Button("查找空文件夹"))
        {
            FindEmptyFolders();
        }

        if (emptyFolders.Count > 0)
        {
            GUILayout.Label($"找到 {emptyFolders.Count} 个空文件夹:");
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            foreach (var folderPath in emptyFolders)
            {
                EditorGUILayout.LabelField(folderPath);
            }
            EditorGUILayout.EndScrollView();

            if (!showConfirmation)
            {
                if (GUILayout.Button("删除空文件夹"))
                {
                    showConfirmation = true;
                }
            }
            else
            {
                GUILayout.Label("确定要删除这些空文件夹吗？此操作不可撤销！");
                
                if (GUILayout.Button("确认删除"))
                {
                    DeleteEmptyFolders();
                    showConfirmation = false;
                }
                
                if (GUILayout.Button("取消"))
                {
                    showConfirmation = false;
                }
            }
        }
        else if (GUILayout.Button("已查找但未找到空文件夹"))
        {
            EditorGUILayout.HelpBox("没有找到空文件夹，或者尚未执行查找操作。", MessageType.Info);
        }
    }

    void FindEmptyFolders()
    {
        emptyFolders.Clear();
        
        string[] allDirectories = Directory.GetDirectories("Assets", "*", SearchOption.AllDirectories);
        
        foreach (string directory in allDirectories)
        {
            // 检查文件夹是否为空（不包含文件或子文件夹）
            if (IsDirectoryEmpty(directory))
            {
                emptyFolders.Add(directory);
            }
        }
        
        EditorUtility.DisplayDialog("完成", $"找到 {emptyFolders.Count} 个空文件夹", "确定");
    }

    bool IsDirectoryEmpty(string path)
    {
        // 检查是否有文件
        if (Directory.GetFiles(path).Length > 0)
            return false;
            
        // 检查是否有非空子文件夹
        foreach (string subDir in Directory.GetDirectories(path))
        {
            if (!IsDirectoryEmpty(subDir))
                return false;
        }
        
        return true;
    }

    void DeleteEmptyFolders()
    {
        int successCount = 0;
        int failCount = 0;
        
        // 从最深层的文件夹开始删除
        emptyFolders.Sort((a, b) => b.Length.CompareTo(a.Length));
        
        foreach (string folderPath in emptyFolders)
        {
            try
            {
                // 删除.meta文件
                string metaFile = folderPath + ".meta";
                if (File.Exists(metaFile))
                {
                    File.Delete(metaFile);
                }
                
                // 删除文件夹
                Directory.Delete(folderPath);
                
                successCount++;
            }
            catch (System.Exception e)
            {
                failCount++;
                Debug.LogError($"无法删除文件夹 {folderPath}: {e.Message}");
            }
        }
        
        emptyFolders.Clear();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("完成", 
            $"已删除 {successCount} 个空文件夹\n{failCount} 个文件夹删除失败", 
            "确定");
    }
}