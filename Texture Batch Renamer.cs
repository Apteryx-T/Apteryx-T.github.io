using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class TextureBatchRenamer : EditorWindow
{
    private string searchFolder = "Assets/";
    private bool includeSubFolders = true;
    private bool showPreview = true;
    private Vector2 scrollPos;
    
    private List<RenameInfo> renameList = new List<RenameInfo>();
    
    [MenuItem("Tools/Texture Batch Renamer")]
    public static void ShowWindow()
    {
        GetWindow<TextureBatchRenamer>("Texture Batch Renamer");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Texture Renaming Settings", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        searchFolder = EditorGUILayout.TextField("Search Folder:", searchFolder);
        includeSubFolders = EditorGUILayout.Toggle("Include Subfolders", includeSubFolders);
        showPreview = EditorGUILayout.Toggle("Show Preview", showPreview);
        
        EditorGUILayout.Space();
        if (GUILayout.Button("Scan Materials"))
        {
            ScanMaterials();
        }
        
        if (GUILayout.Button("Execute Rename"))
        {
            ExecuteRename();
        }
        
        if (showPreview && renameList.Count > 0)
        {
            EditorGUILayout.Space();
            GUILayout.Label("Preview Rename Operations (" + renameList.Count + "):", EditorStyles.boldLabel);
            
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            foreach (var item in renameList)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(item.oldName, GUILayout.Width(200));
                EditorGUILayout.LabelField("→", GUILayout.Width(30), GUILayout.ExpandWidth(false));
                EditorGUILayout.LabelField(item.newName, GUILayout.Width(200));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }
    }
    
    private void ScanMaterials()
    {
        renameList.Clear();
        
        string[] materialPaths = AssetDatabase.FindAssets("t:Material", 
            includeSubFolders ? new[] { searchFolder } : new[] { searchFolder });
        
        foreach (string guid in materialPaths)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            ProcessMaterial(mat);
        }
        
        Debug.Log("Scan complete. Found " + renameList.Count + " textures to rename.");
    }
    
    private void ProcessMaterial(Material mat)
    {
        SerializedObject so = new SerializedObject(mat);
        SerializedProperty texProps = so.FindProperty("m_SavedProperties.m_TexEnvs");
        
        for (int i = 0; i < texProps.arraySize; i++)
        {
            SerializedProperty texProp = texProps.GetArrayElementAtIndex(i);
            string propName = texProp.displayName;
            
            Texture tex = mat.GetTexture(propName);
            if (tex == null) continue;
            
            string texPath = AssetDatabase.GetAssetPath(tex);
            string oldName = Path.GetFileNameWithoutExtension(texPath);
            string ext = Path.GetExtension(texPath);
            
            // Get suffix after last underscore
            int lastUnderscore = oldName.LastIndexOf('_');
            string suffix = lastUnderscore >= 0 ? oldName.Substring(lastUnderscore) : "";
            
            // Create new name pattern: T + _ + [material name without first part] + suffix
            string newName = GenerateNewName(mat.name, suffix);
            
            if (newName != oldName)
            {
                renameList.Add(new RenameInfo {
                    path = texPath,
                    oldName = oldName + ext,
                    newName = newName + ext
                });
            }
        }
    }
    
    private string GenerateNewName(string materialName, string suffix)
    {
        // Split material name by underscores
        string[] parts = materialName.Split('_');
        
        // Rebuild name with "T" as first part
        string newName = "T";
        for (int i = 1; i < parts.Length; i++)
        {
            newName += "_" + parts[i];
        }
        
        // Add original suffix
        newName += suffix;
        
        return newName;
    }
    
    private void ExecuteRename()
    {
        if (renameList.Count == 0)
        {
            Debug.LogWarning("No textures to rename. Scan materials first.");
            return;
        }
        
        int success = 0;
        int failed = 0;
        
        foreach (var item in renameList)
        {
            string result = AssetDatabase.RenameAsset(item.path, Path.GetFileNameWithoutExtension(item.newName));
            if (string.IsNullOrEmpty(result))
            {
                success++;
                Debug.Log("Renamed: " + item.oldName + " → " + item.newName);
            }
            else
            {
                failed++;
                Debug.LogError("Failed to rename " + item.oldName + ": " + result);
            }
        }
        
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Rename Complete", 
            $"Successfully renamed {success} textures.\nFailed: {failed}", "OK");
        
        renameList.Clear();
    }
    
    private struct RenameInfo
    {
        public string path;
        public string oldName;
        public string newName;
    }
}