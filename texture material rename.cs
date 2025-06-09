using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class FolderSelectionBatchRenamer : EditorWindow
{
    private enum RenameTarget
    {
        Textures,
        Materials,
        Both
    }

    private RenameTarget renameTarget = RenameTarget.Textures;
    private bool showPreview = true;
    private Vector2 scrollPos;
    
    // 命名规则选项
    private string texturePrefix = "T_";
    private string materialPrefix = "M_";
    private bool removeNumberSuffix = true;
    private bool smartUnderscoreHandling = true;
    private bool syncTextureWithMaterial = true;
    
    // 文本操作选项
    private string insertText = "";
    private int insertPosition = 0;
    private string findText = "";
    private string replaceText = "";
    private bool useRegex = false;
    
    private List<RenameInfo> renameList = new List<RenameInfo>();
    
    [MenuItem("Tools/Folder Selection Batch Renamer")]
    public static void ShowWindow()
    {
        GetWindow<FolderSelectionBatchRenamer>("Folder Batch Renamer");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Folder Selection Batch Renaming", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Please select folder(s) in Project window first", MessageType.Info);
        
        renameTarget = (RenameTarget)EditorGUILayout.EnumPopup("Target Type:", renameTarget);
        showPreview = EditorGUILayout.Toggle("Show Preview", showPreview);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Naming Rules", EditorStyles.boldLabel);
        
        texturePrefix = EditorGUILayout.TextField("Texture Prefix:", texturePrefix);
        materialPrefix = EditorGUILayout.TextField("Material Prefix:", materialPrefix);
        
        syncTextureWithMaterial = EditorGUILayout.Toggle("Sync Texture with Material", syncTextureWithMaterial);
        removeNumberSuffix = EditorGUILayout.Toggle("Remove Number Suffix", removeNumberSuffix);
        smartUnderscoreHandling = EditorGUILayout.Toggle("Smart Underscore Handling", smartUnderscoreHandling);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Text Operations", EditorStyles.boldLabel);
        
        insertText = EditorGUILayout.TextField("Text to Insert:", insertText);
        insertPosition = EditorGUILayout.IntSlider("Insert Position:", insertPosition, 0, 50);
        
        findText = EditorGUILayout.TextField("Find Text:", findText);
        replaceText = EditorGUILayout.TextField("Replace With:", replaceText);
        useRegex = EditorGUILayout.Toggle("Use Regex", useRegex);
        
        EditorGUILayout.Space();
        if (GUILayout.Button("Scan Selected Folders"))
        {
            ScanSelectedFolders();
        }
        
        if (GUILayout.Button("Execute Rename"))
        {
            ExecuteRename();
        }
        
        DisplayPreview();
    }
    
    private void ScanSelectedFolders()
    {
        renameList.Clear();
        
        // 获取当前选择的文件夹
        List<string> selectedFolders = GetSelectedFolders();
        if (selectedFolders.Count == 0)
        {
            EditorUtility.DisplayDialog("No Folders Selected", "Please select folder(s) in Project window first.", "OK");
            return;
        }

        switch (renameTarget)
        {
            case RenameTarget.Textures:
                ScanTextures(selectedFolders);
                break;
            case RenameTarget.Materials:
                ScanMaterials(selectedFolders);
                break;
            case RenameTarget.Both:
                ScanMaterials(selectedFolders);
                ScanTextures(selectedFolders);
                break;
        }
        
        Debug.Log("Scan complete. Found " + renameList.Count + " items to rename.");
    }
    
    private List<string> GetSelectedFolders()
    {
        List<string> folders = new List<string>();
        
        foreach (Object obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (Directory.Exists(path))
            {
                folders.Add(path);
            }
        }
        
        return folders;
    }
    
    private void ScanTextures(List<string> folders)
    {
        string[] texturePaths = AssetDatabase.FindAssets("t:Texture", folders.ToArray());
        
        foreach (string guid in texturePaths)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Texture tex = AssetDatabase.LoadAssetAtPath<Texture>(path);
            
            if (IsValidAsset(tex, path))
            {
                string oldName = Path.GetFileNameWithoutExtension(path);
                string ext = Path.GetExtension(path);
                string newName = GenerateNewName(oldName, texturePrefix);
                
                // 如果启用同步，并且贴图被材质使用，则基于材质命名
                if (syncTextureWithMaterial)
                {
                    string materialBasedName = GetNameBasedOnMaterialUser(tex, folders);
                    if (!string.IsNullOrEmpty(materialBasedName))
                    {
                        newName = materialBasedName;
                    }
                }
                
                if (newName != oldName)
                {
                    renameList.Add(new RenameInfo {
                        path = path,
                        oldName = oldName + ext,
                        newName = newName + ext,
                        assetObject = tex,
                        assetType = AssetType.Texture
                    });
                }
            }
        }
    }
    
    private void ScanMaterials(List<string> folders)
    {
        string[] materialPaths = AssetDatabase.FindAssets("t:Material", folders.ToArray());
        
        foreach (string guid in materialPaths)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            
            if (IsValidAsset(mat, path))
            {
                string oldName = Path.GetFileNameWithoutExtension(path);
                string ext = Path.GetExtension(path);
                string newName = GenerateNewName(oldName, materialPrefix);
                
                if (newName != oldName)
                {
                    renameList.Add(new RenameInfo {
                        path = path,
                        oldName = oldName + ext,
                        newName = newName + ext,
                        assetObject = mat,
                        assetType = AssetType.Material
                    });
                }
            }
        }
    }
    
    private string GetNameBasedOnMaterialUser(Texture texture, List<string> searchFolders)
    {
        string[] materialPaths = AssetDatabase.FindAssets("t:Material", searchFolders.ToArray());
        foreach (string guid in materialPaths)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            
            if (mat != null && mat.mainTexture == texture)
            {
                string materialName = Path.GetFileNameWithoutExtension(path);
                string newName = GenerateNewName(materialName, texturePrefix)
                    .Replace(materialPrefix, texturePrefix);
                return newName;
            }
        }
        return null;
    }
    
    private bool IsValidAsset(Object obj, string path)
    {
        if (obj == null)
        {
            Debug.LogWarning($"Skipping null asset at path: {path}");
            return false;
        }
        
        if ((obj.hideFlags & HideFlags.DontSaveInEditor) != 0)
        {
            Debug.LogWarning($"Skipping asset marked with DontSaveInEditor: {path}");
            return false;
        }
        
        if (AssetDatabase.IsSubAsset(obj) && path.StartsWith("Resources/unity_builtin_extra"))
        {
            Debug.LogWarning($"Skipping built-in asset: {path}");
            return false;
        }
        
        return true;
    }
    
    private string GenerateNewName(string originalName, string prefix)
    {
        string newName = originalName;
        
        // 应用查找替换
        if (!string.IsNullOrEmpty(findText))
        {
            if (useRegex)
            {
                try
                {
                    newName = Regex.Replace(newName, findText, replaceText);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Invalid regex pattern: {e.Message}");
                }
            }
            else
            {
                newName = newName.Replace(findText, replaceText);
            }
        }
        
        // 插入文本
        if (!string.IsNullOrEmpty(insertText))
        {
            int pos = Mathf.Clamp(insertPosition, 0, newName.Length);
            newName = newName.Insert(pos, insertText);
        }
        
        // 移除数字后缀
        if (removeNumberSuffix)
        {
            newName = Regex.Replace(newName, @"_\d+$", "");
        }
        
        // 添加前缀
        if (!string.IsNullOrEmpty(prefix))
        {
            if (smartUnderscoreHandling)
            {
                bool needsUnderscore = !prefix.EndsWith("_") && !newName.StartsWith("_");
                newName = prefix + (needsUnderscore ? "_" : "") + newName;
            }
            else
            {
                newName = prefix + newName;
            }
        }
        
        // 处理下划线
        if (smartUnderscoreHandling)
        {
            newName = Regex.Replace(newName, @"_+", "_");
            newName = newName.Trim('_');
        }
        
        return newName;
    }
    
    private void ExecuteRename()
    {
        if (renameList.Count == 0)
        {
            Debug.LogWarning("No items to rename. Scan folders first.");
            return;
        }
        
        int success = 0;
        int failed = 0;
        
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var item in renameList)
            {
                try
                {
                    if (!IsValidAsset(item.assetObject, item.path))
                    {
                        failed++;
                        continue;
                    }
                    
                    string result = AssetDatabase.RenameAsset(item.path, Path.GetFileNameWithoutExtension(item.newName));
                    
                    if (string.IsNullOrEmpty(result))
                    {
                        // 更新材质球内部引用
                        if (item.assetType == AssetType.Material)
                        {
                            Material mat = (Material)item.assetObject;
                            Undo.RecordObject(mat, "Rename Material");
                            EditorUtility.SetDirty(mat);
                        }
                        
                        success++;
                        Debug.Log("Renamed: " + item.oldName + " → " + item.newName);
                    }
                    else
                    {
                        failed++;
                        Debug.LogError("Failed to rename " + item.oldName + ": " + result);
                    }
                }
                catch (System.Exception e)
                {
                    failed++;
                    Debug.LogError($"Error renaming {item.oldName}: {e.Message}");
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("Rename Complete", 
            $"Successfully renamed {success} items.\nFailed: {failed}", "OK");
        
        renameList.Clear();
    }
    
    private void DisplayPreview()
    {
        if (!showPreview || renameList.Count == 0) return;

        EditorGUILayout.Space();
        GUILayout.Label("Preview Rename Operations (" + renameList.Count + "):", EditorStyles.boldLabel);
        
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        foreach (var item in renameList)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(item.oldName, GUILayout.Width(200));
            EditorGUILayout.LabelField("→", GUILayout.Width(30), GUILayout.ExpandWidth(false));
            EditorGUILayout.LabelField(item.newName, GUILayout.Width(200));
            EditorGUILayout.LabelField(item.assetType.ToString(), GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }
    
    private struct RenameInfo
    {
        public string path;
        public string oldName;
        public string newName;
        public Object assetObject;
        public AssetType assetType;
    }
    
    private enum AssetType
    {
        Texture,
        Material
    }
}