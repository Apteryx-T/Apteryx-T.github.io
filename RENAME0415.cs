using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class AdvancedTextureRenameTool : EditorWindow
{
    // 插入配置
    private bool enableInsert = true;
    private string insertText = "";
    private bool autoAddUnderscore = true;
    
    // 替换配置
    private bool enableReplace = false;
    private string replaceFrom = "";
    private string replaceTo = "";
    private bool replaceAllOccurrences = true;
    
    // 预览相关
    private Vector2 scrollPosition;
    private List<RenamePreview> renamePreviews = new List<RenamePreview>();
    private bool showOnlyModified = true;

    [MenuItem("Tools/高级贴图重命名工具")]
    public static void ShowWindow()
    {
        GetWindow<AdvancedTextureRenameTool>("贴图重命名工具").minSize = new Vector2(500, 450);
    }

    private void OnGUI()
    {
        GUILayout.Label("高级贴图重命名工具", EditorStyles.boldLabel);
        
        EditorGUI.BeginChangeCheck();
        
        // 插入功能配置
        EditorGUILayout.Space();
        enableInsert = EditorGUILayout.BeginToggleGroup(new GUIContent("插入文本", "在T_后插入指定文本"), enableInsert);
        if (enableInsert)
        {
            EditorGUI.indentLevel++;
            insertText = EditorGUILayout.TextField("插入内容", insertText);
            autoAddUnderscore = EditorGUILayout.Toggle("自动添加下划线", autoAddUnderscore);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndToggleGroup();
        
        // 替换功能配置
        EditorGUILayout.Space();
        enableReplace = EditorGUILayout.BeginToggleGroup(new GUIContent("替换文本", "替换文件名中的指定文本"), enableReplace);
        if (enableReplace)
        {
            EditorGUI.indentLevel++;
            replaceFrom = EditorGUILayout.TextField("查找内容", replaceFrom);
            replaceTo = EditorGUILayout.TextField("替换为", replaceTo);
            replaceAllOccurrences = EditorGUILayout.Toggle("替换所有匹配项", replaceAllOccurrences);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndToggleGroup();

        // 预览配置
        EditorGUILayout.Space();
        showOnlyModified = EditorGUILayout.Toggle("仅显示修改项", showOnlyModified);

        if (EditorGUI.EndChangeCheck())
        {
            UpdatePreviews();
        }

        // 预览区域
        EditorGUILayout.Space();
        GUILayout.Label("重命名预览", EditorStyles.boldLabel);
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(250));
        if (renamePreviews.Count == 0)
        {
            GUILayout.Label("没有可预览的修改，请选择贴图文件");
        }
        else
        {
            foreach (var preview in renamePreviews)
            {
                if (!showOnlyModified || preview.originalName != preview.newName)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label(preview.originalName, GUILayout.Width(200));
                    GUILayout.Label("→", GUILayout.Width(20));
                    
                    if (preview.originalName == preview.newName)
                    {
                        GUI.color = Color.gray;
                        GUILayout.Label("(无变化)", GUILayout.Width(200));
                        GUI.color = Color.white;
                    }
                    else
                    {
                        GUILayout.Label(preview.newName, GUILayout.Width(200));
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
        EditorGUILayout.EndScrollView();

        // 操作按钮
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("刷新预览", GUILayout.Height(30)))
        {
            UpdatePreviews();
        }
        
        GUI.enabled = GetModifiedCount() > 0;
        if (GUILayout.Button("应用重命名", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("确认重命名", 
                $"确定要重命名 {GetModifiedCount()} 个文件吗？", "确定", "取消"))
            {
                ApplyRenames();
            }
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
    }

    private int GetModifiedCount()
    {
        int count = 0;
        foreach (var preview in renamePreviews)
        {
            if (preview.originalName != preview.newName)
            {
                count++;
            }
        }
        return count;
    }

    private void OnSelectionChange()
    {
        UpdatePreviews();
        Repaint();
    }

    private void UpdatePreviews()
    {
        renamePreviews.Clear();
        
        Object[] selectedTextures = Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets);
        
        foreach (Object texObj in selectedTextures)
        {
            string path = AssetDatabase.GetAssetPath(texObj);
            string filename = Path.GetFileNameWithoutExtension(path);
            string newName = filename;

            // 处理插入功能
            if (enableInsert && !string.IsNullOrEmpty(insertText) && newName.Contains("T_"))
            {
                int insertPosition = newName.IndexOf("T_") + 2;
                string insertContent = autoAddUnderscore ? insertText + "_" : insertText;
                newName = newName.Insert(insertPosition, insertContent);
            }

            // 处理替换功能
            if (enableReplace && !string.IsNullOrEmpty(replaceFrom))
            {
                if (replaceAllOccurrences)
                {
                    newName = newName.Replace(replaceFrom, replaceTo);
                }
                else
                {
                    int pos = newName.IndexOf(replaceFrom);
                    if (pos >= 0)
                    {
                        newName = newName.Remove(pos, replaceFrom.Length).Insert(pos, replaceTo);
                    }
                }
            }

            renamePreviews.Add(new RenamePreview
            {
                originalPath = path,
                originalName = filename,
                newName = newName
            });
        }
    }

    private void ApplyRenames()
    {
        int successCount = 0;
        int failCount = 0;

        foreach (var preview in renamePreviews)
        {
            if (preview.originalName == preview.newName) continue;

            string directory = Path.GetDirectoryName(preview.originalPath);
            string extension = Path.GetExtension(preview.originalPath);
            string newPath = Path.Combine(directory, preview.newName + extension);

            if (File.Exists(newPath))
            {
                Debug.LogWarning($"跳过 {preview.originalName}，因为 {preview.newName} 已存在");
                failCount++;
                continue;
            }

            string error = AssetDatabase.RenameAsset(preview.originalPath, preview.newName);
            if (string.IsNullOrEmpty(error))
            {
                successCount++;
            }
            else
            {
                Debug.LogError($"重命名 {preview.originalName} 失败: {error}");
                failCount++;
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"重命名完成！成功: {successCount}, 失败: {failCount}");
        UpdatePreviews();
    }

    private class RenamePreview
    {
        public string originalPath;
        public string originalName;
        public string newName;
    }
}