using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;

public class ModelRenamer : EditorWindow
{
    private string searchPattern = "";
    private string replacePattern = "";
    private string insertPattern = "";
    private int insertPosition = 0;
    private string removePattern = "";
    private bool useRegex = false;
    private bool renameFiles = false;
    private bool previewOnly = true;
    private bool includeSubfolders = false;
    
    private List<string> originalNames = new List<string>();
    private List<string> newNames = new List<string>();
    
    private Vector2 scrollPos;
    
    [MenuItem("Tools/模型批量重命名")]
    public static void ShowWindow()
    {
        GetWindow<ModelRenamer>("模型重命名工具");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("模型重命名工具", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("查找和替换", EditorStyles.boldLabel);
        searchPattern = EditorGUILayout.TextField("查找内容:", searchPattern);
        replacePattern = EditorGUILayout.TextField("替换为:", replacePattern);
        useRegex = EditorGUILayout.Toggle("使用正则表达式", useRegex);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("插入字符", EditorStyles.boldLabel);
        insertPattern = EditorGUILayout.TextField("插入内容:", insertPattern);
        insertPosition = EditorGUILayout.IntField("插入位置:", insertPosition);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("删除字符", EditorStyles.boldLabel);
        removePattern = EditorGUILayout.TextField("删除内容:", removePattern);
        
        EditorGUILayout.Space();
        renameFiles = EditorGUILayout.Toggle("同时重命名文件", renameFiles);
        previewOnly = EditorGUILayout.Toggle("仅预览", previewOnly);
        includeSubfolders = EditorGUILayout.Toggle("包含子文件夹", includeSubfolders);
        
        EditorGUILayout.Space();
        if (GUILayout.Button("处理选中模型"))
        {
            ProcessSelectedModels();
        }
        
        EditorGUILayout.Space();
        if (originalNames.Count > 0)
        {
            EditorGUILayout.LabelField("重命名预览:", EditorStyles.boldLabel);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
            
            for (int i = 0; i < originalNames.Count; i++)
            {
                EditorGUILayout.LabelField($"{originalNames[i]} → {newNames[i]}");
            }
            
            EditorGUILayout.EndScrollView();
            
            if (!previewOnly && GUILayout.Button("应用重命名"))
            {
                ApplyRenaming();
            }
        }
    }
    
    private void ProcessSelectedModels()
    {
        originalNames.Clear();
        newNames.Clear();
        
        foreach (var obj in Selection.objects)
        {
            if (obj is GameObject)
            {
                string originalName = obj.name;
                string newName = originalName;
                
                // 执行替换操作
                if (!string.IsNullOrEmpty(searchPattern))
                {
                    if (useRegex)
                    {
                        try
                        {
                            newName = Regex.Replace(newName, searchPattern, replacePattern);
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"正则表达式错误: {e.Message}");
                            return;
                        }
                    }
                    else
                    {
                        newName = newName.Replace(searchPattern, replacePattern);
                    }
                }
                
                // 执行插入操作
                if (!string.IsNullOrEmpty(insertPattern))
                {
                    if (insertPosition >= 0 && insertPosition <= newName.Length)
                    {
                        newName = newName.Insert(insertPosition, insertPattern);
                    }
                    else
                    {
                        Debug.LogWarning($"插入位置 {insertPosition} 超出范围 (0-{newName.Length})");
                    }
                }
                
                // 执行删除操作
                if (!string.IsNullOrEmpty(removePattern))
                {
                    if (useRegex)
                    {
                        try
                        {
                            newName = Regex.Replace(newName, removePattern, "");
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"正则表达式错误: {e.Message}");
                            return;
                        }
                    }
                    else
                    {
                        newName = newName.Replace(removePattern, "");
                    }
                }
                
                if (originalName != newName)
                {
                    originalNames.Add(originalName);
                    newNames.Add(newName);
                }
            }
        }
        
        if (renameFiles)
        {
            ProcessModelFiles();
        }
    }
    
    private void ProcessModelFiles()
    {
        string[] selectedPaths = new string[Selection.assetGUIDs.Length];
        for (int i = 0; i < Selection.assetGUIDs.Length; i++)
        {
            selectedPaths[i] = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[i]);
        }
        
        foreach (string path in selectedPaths)
        {
            if (Path.GetExtension(path).ToLower() == ".fbx" || 
                Path.GetExtension(path).ToLower() == ".obj" ||
                Path.GetExtension(path).ToLower() == ".blend")
            {
                string originalName = Path.GetFileNameWithoutExtension(path);
                string newName = originalName;
                
                // 执行替换操作
                if (!string.IsNullOrEmpty(searchPattern))
                {
                    if (useRegex)
                    {
                        try
                        {
                            newName = Regex.Replace(newName, searchPattern, replacePattern);
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"正则表达式错误: {e.Message}");
                            return;
                        }
                    }
                    else
                    {
                        newName = newName.Replace(searchPattern, replacePattern);
                    }
                }
                
                // 执行插入操作
                if (!string.IsNullOrEmpty(insertPattern))
                {
                    if (insertPosition >= 0 && insertPosition <= newName.Length)
                    {
                        newName = newName.Insert(insertPosition, insertPattern);
                    }
                    else
                    {
                        Debug.LogWarning($"插入位置 {insertPosition} 超出范围 (0-{newName.Length})");
                    }
                }
                
                // 执行删除操作
                if (!string.IsNullOrEmpty(removePattern))
                {
                    if (useRegex)
                    {
                        try
                        {
                            newName = Regex.Replace(newName, removePattern, "");
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"正则表达式错误: {e.Message}");
                            return;
                        }
                    }
                    else
                    {
                        newName = newName.Replace(removePattern, "");
                    }
                }
                
                if (originalName != newName)
                {
                    originalNames.Add(originalName);
                    newNames.Add(newName);
                }
            }
        }
    }
    
    private void ApplyRenaming()
    {
        // 重命名场景中的GameObject
        for (int i = 0; i < originalNames.Count; i++)
        {
            foreach (var obj in Selection.objects)
            {
                if (obj is GameObject && obj.name == originalNames[i])
                {
                    obj.name = newNames[i];
                    break;
                }
            }
        }
        
        // 重命名文件
        if (renameFiles)
        {
            string[] selectedPaths = new string[Selection.assetGUIDs.Length];
            for (int i = 0; i < Selection.assetGUIDs.Length; i++)
            {
                selectedPaths[i] = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[i]);
            }
            
            for (int i = 0; i < originalNames.Count; i++)
            {
                foreach (string path in selectedPaths)
                {
                    if (Path.GetExtension(path).ToLower() == ".fbx" || 
                        Path.GetExtension(path).ToLower() == ".obj" ||
                        Path.GetExtension(path).ToLower() == ".blend")
                    {
                        string fileName = Path.GetFileNameWithoutExtension(path);
                        if (fileName == originalNames[i])
                        {
                            string newPath = Path.Combine(Path.GetDirectoryName(path), newNames[i] + Path.GetExtension(path));
                            string error = AssetDatabase.RenameAsset(path, newNames[i]);
                            if (!string.IsNullOrEmpty(error))
                            {
                                Debug.LogError($"重命名文件失败: {error}");
                            }
                            break;
                        }
                    }
                }
            }
        }
        
        AssetDatabase.Refresh();
        originalNames.Clear();
        newNames.Clear();
    }
}