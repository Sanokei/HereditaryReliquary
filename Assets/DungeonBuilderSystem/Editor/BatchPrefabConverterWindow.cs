using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DungeonBuilderSystem.Editor
{
    /// <summary>
    /// Converts model assets (FBX/OBJ/etc.) inside a folder into prefabs.
    /// </summary>
    public class BatchPrefabConverterWindow : EditorWindow
    {
        private DefaultAsset sourceFolder;
        private DefaultAsset outputFolder;
        private bool preserveSubFolders = true;
        private bool alignBottomToZero = true;
        private int gridCellSize = 1;

        private readonly List<string> supportedExtensions = new()
        {
            ".fbx", ".obj", ".dae", ".blend", ".glb", ".gltf"
        };

        [MenuItem("Dungeon Builder/Batch Prefab Converter")]
        public static void ShowWindow()
        {
            var window = GetWindow<BatchPrefabConverterWindow>("Prefab Converter");
            window.minSize = new Vector2(420f, 230f);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Model → Prefab Converter", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Converts all model assets in a folder (and optional sub-folders) into prefabs.", MessageType.Info);

            sourceFolder = (DefaultAsset)EditorGUILayout.ObjectField("Source Folder", sourceFolder, typeof(DefaultAsset), false);
            outputFolder = (DefaultAsset)EditorGUILayout.ObjectField("Output Folder", outputFolder, typeof(DefaultAsset), false);

            preserveSubFolders = EditorGUILayout.Toggle("Preserve Sub-Folders", preserveSubFolders);
            alignBottomToZero = EditorGUILayout.Toggle("Align Models To Ground", alignBottomToZero);
            gridCellSize = Mathf.Max(1, EditorGUILayout.IntField("Reference Cell Size", gridCellSize));

            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(sourceFolder == null || outputFolder == null))
            {
                if (GUILayout.Button("Convert Models", GUILayout.Height(32f)))
                {
                    ConvertModels();
                }
            }
        }

        private void ConvertModels()
        {
            string sourcePath = GetFolderAssetPath(sourceFolder);
            string outputPath = GetFolderAssetPath(outputFolder);

            if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(outputPath))
            {
                EditorUtility.DisplayDialog("Invalid Paths", "Both source and output folders must be assigned.", "OK");
                return;
            }

            string[] modelGuids = AssetDatabase.FindAssets("t:GameObject", new[] { sourcePath });
            var modelPaths = new List<string>();

            foreach (string guid in modelGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string extension = Path.GetExtension(assetPath).ToLowerInvariant();
                AssetImporter importer = AssetImporter.GetAtPath(assetPath);

                if (importer is ModelImporter && supportedExtensions.Contains(extension))
                {
                    modelPaths.Add(assetPath);
                }
            }

            if (modelPaths.Count == 0)
            {
                EditorUtility.DisplayDialog("No Models Found", "No supported model assets were found in the selected folder.", "OK");
                return;
            }

            int converted = 0;
            try
            {
                for (int i = 0; i < modelPaths.Count; i++)
                {
                    string modelPath = modelPaths[i];
                    EditorUtility.DisplayProgressBar("Converting Models", modelPath, i / (float)modelPaths.Count);
                    ConvertSingleModel(modelPath, sourcePath, outputPath);
                    converted++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Conversion Complete", $"Converted {converted} model(s) to prefabs.", "OK");
        }

        private void ConvertSingleModel(string modelPath, string sourceRoot, string outputRoot)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null)
                return;

            string targetFolder = outputRoot;
            if (preserveSubFolders)
            {
                string modelDirectory = Path.GetDirectoryName(modelPath).Replace("\\", "/");
                string relative = modelDirectory.Length > sourceRoot.Length
                    ? modelDirectory.Substring(sourceRoot.Length).TrimStart('/')
                    : string.Empty;

                targetFolder = Path.Combine(outputRoot, relative).Replace("\\", "/");
            }

            EnsureFolderExists(targetFolder);

            string prefabPath = Path.Combine(targetFolder, model.name + ".prefab").Replace("\\", "/");
            prefabPath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);

            GameObject container = new GameObject(model.name);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            instance.transform.SetParent(container.transform);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            if (alignBottomToZero)
            {
                AlignInstanceToGround(instance);
            }

            // Optional: scale reference to grid cell size (uniform)
            if (gridCellSize > 0)
            {
                float scaleFactor = 1f; // Placeholder for future scaling rules
                instance.transform.localScale = Vector3.one * scaleFactor;
            }

            PrefabUtility.SaveAsPrefabAsset(container, prefabPath);
            DestroyImmediate(container);
        }

        private static void AlignInstanceToGround(GameObject instance)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers == null || renderers.Length == 0)
                return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            Vector3 offset = -bounds.center;
            offset.y += bounds.extents.y; // move bottom to Y = 0
            instance.transform.localPosition = offset;
        }

        private static void EnsureFolderExists(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
                return;

            string parent = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
            string folderName = Path.GetFileName(assetPath);

            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolderExists(parent);
            }

            AssetDatabase.CreateFolder(parent ?? "Assets", folderName);
        }

        private static string GetFolderAssetPath(DefaultAsset folderAsset)
        {
            if (folderAsset == null)
                return null;

            string assetPath = AssetDatabase.GetAssetPath(folderAsset);
            if (Directory.Exists(assetPath))
            {
                return assetPath.Replace("\\", "/");
            }

            return null;
        }
    }
}

