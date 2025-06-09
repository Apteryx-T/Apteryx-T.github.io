using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class RenameTexturesToMatchMaterial : EditorWindow
{
    private Dictionary<Texture2D, string> previewRenames = new Dictionary<Texture2D, string>();
    private Vector2 scrollPosition;

    [MenuItem("Tools/Rename Textures to Match Material")]
    public static void ShowWindow()
    {
        GetWindow<RenameTexturesToMatchMaterial>("Rename Textures");
    }

    private void OnGUI()
    {
        GUILayout.Label("Rename Textures to Match Material Name", EditorStyles.boldLabel);

        if (GUILayout.Button("Preview Selected Textures"))
        {
            PreviewSelectedTextures();
        }

        if (previewRenames.Count > 0)
        {
            GUILayout.Space(10);
            GUILayout.Label("Preview Renames:", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
            foreach (var pair in previewRenames)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(pair.Key, typeof(Texture2D), false, GUILayout.Width(150));
                GUILayout.Label("→", GUILayout.ExpandWidth(false));
                EditorGUILayout.TextField(pair.Value, GUILayout.Width(150));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Apply Renames"))
            {
                ApplyRenames();
                previewRenames.Clear();
            }

            if (GUILayout.Button("Clear Preview"))
            {
                previewRenames.Clear();
            }
        }
    }

    private void PreviewSelectedTextures()
    {
        previewRenames.Clear();
        Object[] selectedTextures = Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets);

        if (selectedTextures.Length == 0)
        {
            Debug.LogWarning("No textures selected. Please select textures in the Project window.");
            return;
        }

        foreach (Object texObj in selectedTextures)
        {
            Texture2D texture = (Texture2D)texObj;
            string texturePath = AssetDatabase.GetAssetPath(texture);
            string textureName = Path.GetFileNameWithoutExtension(texturePath);

            int lastUnderscoreIndex = textureName.LastIndexOf('_');
            string suffix = (lastUnderscoreIndex >= 0) ? textureName.Substring(lastUnderscoreIndex) : "";

            string[] materialGuids = AssetDatabase.FindAssets("t:Material");
            bool foundMatch = false;

            foreach (string guid in materialGuids)
            {
                string materialPath = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

                if (material.mainTexture == texture || HasTextureInProperties(material, texture))
                {
                    string newName = material.name + suffix;
                    previewRenames[texture] = newName;
                    foundMatch = true;
                    break;
                }
            }

            if (!foundMatch)
            {
                Debug.LogWarning($"No material found using texture: {texture.name}");
            }
        }

        if (previewRenames.Count > 0)
        {
            Debug.Log($"Preview generated for {previewRenames.Count} textures.");
        }
    }

    private void ApplyRenames()
    {
        if (previewRenames.Count == 0)
        {
            Debug.LogWarning("No preview data to apply.");
            return;
        }

        foreach (var pair in previewRenames)
        {
            string texturePath = AssetDatabase.GetAssetPath(pair.Key);
            string result = AssetDatabase.RenameAsset(texturePath, pair.Value);

            if (string.IsNullOrEmpty(result))
            {
                Debug.Log($"Renamed: {pair.Key.name} -> {pair.Value}");
            }
            else
            {
                Debug.LogError($"Failed to rename {pair.Key.name}: {result}");
            }
        }

        AssetDatabase.Refresh();
        Debug.Log("Renaming completed!");
    }

    private static bool HasTextureInProperties(Material material, Texture2D texture)
    {
        Shader shader = material.shader;
        if (shader == null) return false;

        int propertyCount = ShaderUtil.GetPropertyCount(shader);
        for (int i = 0; i < propertyCount; i++)
        {
            if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
            {
                string propertyName = ShaderUtil.GetPropertyName(shader, i);
                Texture propTexture = material.GetTexture(propertyName);
                if (propTexture == texture)
                {
                    return true;
                }
            }
        }
        return false;
    }
}