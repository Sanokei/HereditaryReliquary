using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using GridBuilder.Core;

namespace DungeonBuilderSystem.Editor
{
    /// <summary>
    /// Adds every prefab in a folder to the selected ObjectsDatabaseSO.
    /// </summary>
    public class ObjectDatabaseImportWindow : EditorWindow
    {
        private ObjectsDatabaseSO targetDatabase;
        private DefaultAsset prefabsFolder;
        private bool includeSubFolders = true;
        private bool overwriteExisting = false;
        private int customIdOffset = 0;

        [MenuItem("Dungeon Builder/Object Database Importer")]
        public static void ShowWindow()
        {
            var window = GetWindow<ObjectDatabaseImportWindow>("Database Importer");
            window.minSize = new Vector2(420f, 230f);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Prefab → Object Database", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Adds every prefab within a folder to an ObjectsDatabase asset.", MessageType.Info);

            targetDatabase = (ObjectsDatabaseSO)EditorGUILayout.ObjectField("Target Database", targetDatabase, typeof(ObjectsDatabaseSO), false);
            prefabsFolder = (DefaultAsset)EditorGUILayout.ObjectField("Prefabs Folder", prefabsFolder, typeof(DefaultAsset), false);

            includeSubFolders = EditorGUILayout.Toggle("Include Sub-Folders", includeSubFolders);
            overwriteExisting = EditorGUILayout.Toggle("Overwrite Matching Names", overwriteExisting);
            customIdOffset = EditorGUILayout.IntField("ID Offset", customIdOffset);

            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(targetDatabase == null || prefabsFolder == null))
            {
                if (GUILayout.Button("Import Prefabs", GUILayout.Height(32f)))
                {
                    ImportPrefabs();
                }
            }
        }

        private void ImportPrefabs()
        {
            string folderPath = GetFolderAssetPath(prefabsFolder);
            if (string.IsNullOrEmpty(folderPath))
            {
                EditorUtility.DisplayDialog("Invalid Folder", "Please assign a valid folder containing prefabs.", "OK");
                return;
            }

            if (targetDatabase.objectsData == null)
            {
                targetDatabase.objectsData = new List<ObjectData>();
            }

            string[] searchFolders = includeSubFolders ? new[] { folderPath } : GetImmediateSubFolders(folderPath);
            if (!includeSubFolders)
            {
                searchFolders = searchFolders.Length == 0 ? new[] { folderPath } : searchFolders;
            }

            HashSet<string> existingNames = new HashSet<string>(targetDatabase.objectsData.Select(x => x.Name));

            List<string> prefabPaths = new List<string>();
            foreach (string searchFolder in searchFolders)
            {
                string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { searchFolder });
                foreach (string guid in prefabGuids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (!prefabPaths.Contains(assetPath))
                    {
                        prefabPaths.Add(assetPath);
                    }
                }
            }

            if (prefabPaths.Count == 0)
            {
                EditorUtility.DisplayDialog("No Prefabs Found", "No prefabs were located in the selected folder.", "OK");
                return;
            }

            int imported = 0;
            try
            {
                for (int i = 0; i < prefabPaths.Count; i++)
                {
                    string path = prefabPaths[i];
                    EditorUtility.DisplayProgressBar("Importing Prefabs", path, i / (float)prefabPaths.Count);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab == null)
                        continue;

                    string entryName = prefab.name;
                    bool exists = existingNames.Contains(entryName);
                    if (exists && !overwriteExisting)
                        continue;

                    ObjectData data = exists
                        ? targetDatabase.objectsData.First(x => x.Name == entryName)
                        : new ObjectData();

                    AssignObjectDataFields(data, entryName, prefab, targetDatabase.CellSize, customIdOffset);

                    if (!exists)
                    {
                        targetDatabase.objectsData.Add(data);
                        existingNames.Add(entryName);
                    }

                    imported++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            EditorUtility.SetDirty(targetDatabase);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("Import Complete", $"Imported {imported} prefab(s) into {targetDatabase.name}.", "OK");
        }

        private static void AssignObjectDataFields(ObjectData data, string name, GameObject prefab, int cellSize, int idOffset)
        {
            SetBackingField(data, "<Name>k__BackingField", name);
            int generatedId = GenerateDeterministicId(name, prefab) + idOffset;
            data.SetID(generatedId);
            data.SetPrefab(prefab, cellSize);
        }

        private static int GenerateDeterministicId(string name, GameObject prefab)
        {
            string hashSource = $"{name}_{prefab.GetInstanceID()}_{prefab.GetHashCode()}";
            return Mathf.Abs(Animator.StringToHash(hashSource));
        }

        private static void SetBackingField<T>(ObjectData data, string fieldName, T value)
        {
            var field = typeof(ObjectData).GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(data, value);
            }
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

        private static string[] GetImmediateSubFolders(string rootFolder)
        {
            if (!AssetDatabase.IsValidFolder(rootFolder))
                return new string[0];

            return AssetDatabase.GetSubFolders(rootFolder);
        }
    }
}

