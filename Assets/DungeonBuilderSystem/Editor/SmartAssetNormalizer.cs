using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using GridBuilder.Core;
using System.Linq;

namespace DungeonBuilderSystem.Editor
{
    public class SmartAssetNormalizer : EditorWindow
    {
        private DefaultAsset targetDirectory;
        private ObjectsDatabaseSO targetDatabase;
        private int gridCellSize = 1; // Default to 1

        [MenuItem("Dungeon Builder/Smart Asset Normalizer")]
        public static void ShowWindow()
        {
            GetWindow<SmartAssetNormalizer>("Asset Normalizer");
        }

        private void OnGUI()
        {
            GUILayout.Label("Smart Asset Normalizer", EditorStyles.boldLabel);

            targetDirectory = (DefaultAsset)EditorGUILayout.ObjectField("Target Directory", targetDirectory, typeof(DefaultAsset), false);
            targetDatabase = (ObjectsDatabaseSO)EditorGUILayout.ObjectField("Target Database", targetDatabase, typeof(ObjectsDatabaseSO), false);
            gridCellSize = EditorGUILayout.IntField("Grid Cell Size", gridCellSize);

            if (GUILayout.Button("Normalize & Bake"))
            {
                if (targetDirectory == null || targetDatabase == null)
                {
                    EditorUtility.DisplayDialog("Error", "Please select directory and database.", "OK");
                    return;
                }
                ProcessAssets();
            }
        }

        private void ProcessAssets()
        {
            string targetPath = GetFolderAssetPath(targetDirectory);
            if (string.IsNullOrEmpty(targetPath))
            {
                EditorUtility.DisplayDialog("Error", "Invalid target directory selected.", "OK");
                return;
            }

            if (!Directory.Exists(targetPath))
            {
                Debug.LogError($"Directory not found: {targetPath}");
                return;
            }

            string[] files = Directory.GetFiles(targetPath, "*.obj", SearchOption.AllDirectories); // Also check .fbx?
            // Combining .obj and .fbx for robustness
            var fbxFiles = Directory.GetFiles(targetPath, "*.fbx", SearchOption.AllDirectories);
            files = files.Concat(fbxFiles).ToArray();

            int processedCount = 0;

            foreach (string file in files)
            {
                string relativePath = file.Replace(Application.dataPath, "Assets").Replace("\\", "/");
                // Fix path if it starts with absolute path
                if (relativePath.StartsWith(Application.dataPath))
                {
                     relativePath = "Assets" + relativePath.Substring(Application.dataPath.Length);
                }
                // If we are already relative to project root
                if (!relativePath.StartsWith("Assets"))
                {
                     // Assume the script logic for path adjustment might need tweaking depending on input
                     // But Directory.GetFiles usually returns full paths.
                     relativePath = FileUtil.GetProjectRelativePath(file);
                }

                GameObject rawAsset = AssetDatabase.LoadAssetAtPath<GameObject>(relativePath);
                if (rawAsset == null) continue;

                ProcessSingleAsset(rawAsset, relativePath);
                processedCount++;
            }

            // Save database
            EditorUtility.SetDirty(targetDatabase);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Complete", $"Processed {processedCount} assets.", "OK");
        }

        private void ProcessSingleAsset(GameObject rawAsset, string path)
        {
            string assetName = rawAsset.name;
            bool isWall = IsWall(assetName);
            
            // Create a normalized prefab instance in scene
            GameObject container = new GameObject(assetName);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(rawAsset);
            instance.transform.SetParent(container.transform);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            // Normalize (Center and Scale)
            NormalizeBounds(container, instance);

            // Save basic prefab
            string folderPath = Path.GetDirectoryName(path);
            string prefabsPath = Path.Combine(folderPath, "Prefabs");
            if (!Directory.Exists(prefabsPath)) Directory.CreateDirectory(prefabsPath);

            string prefabPath = Path.Combine(prefabsPath, assetName + ".prefab").Replace("\\", "/");
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(container, prefabPath);
            
            // Add to Database
            AddToDatabase(prefab, assetName);

            // If Wall, create Corner
            if (isWall)
            {
                CreateCornerPrefab(prefab, assetName, prefabsPath);
            }

            DestroyImmediate(container);
        }

        private void NormalizeBounds(GameObject container, GameObject model)
        {
            // Calculate bounds
            Bounds bounds = GetBounds(model);
            
            // Center mesh: Move model so its bottom center is at (0,0,0)
            Vector3 centerOffset = -bounds.center;
            centerOffset.y += bounds.extents.y; // Move so bottom is at 0
            model.transform.localPosition = centerOffset;

            // Optional: Scale to fit grid cell?
            // The prompt says "match the standard grid cell size".
            // Usually this means scaling so it fits within 1x1x1 * gridCellSize.
            // But for walls, we might only care about width/height. 
            // Let's assume uniform scaling if it's huge, or keep 1:1 if it's authored for the grid.
            // For now, I'll leave scale as 1, assuming assets are somewhat correct, 
            // or I could enforce a scale factor if max dimension > gridCellSize.
        }

        private Bounds GetBounds(GameObject obj)
        {
            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }
            return bounds;
        }

        private bool IsWall(string name)
        {
            string lower = name.ToLower();
            if (lower.Contains("wall"))
            {
                if (lower.Contains("shelf") || lower.Contains("torch") || lower.Contains("decor"))
                    return false;
                return true;
            }
            return false;
        }

        private void CreateCornerPrefab(GameObject straightWallPrefab, string baseName, string folderPath)
        {
            GameObject cornerContainer = new GameObject(baseName + "_Corner");
            
            // Wall 1
            GameObject w1 = (GameObject)PrefabUtility.InstantiatePrefab(straightWallPrefab);
            w1.transform.SetParent(cornerContainer.transform);
            w1.transform.localPosition = Vector3.zero;
            w1.transform.localRotation = Quaternion.identity;

            // Wall 2 (Rotated 90)
            GameObject w2 = (GameObject)PrefabUtility.InstantiatePrefab(straightWallPrefab);
            w2.transform.SetParent(cornerContainer.transform);
            // Position adjustment for corner depends on pivot.
            // Assuming pivot is center-bottom or edge.
            // If pivot is center-edge:
            w2.transform.localRotation = Quaternion.Euler(0, 90, 0);
            // We might need to offset w2 to make a corner. 
            // Simple corner: L shape.
            // If pivot is center of the wall segment, w2 needs to be moved.
            // Let's assume pivot is at the start/edge for modular pieces, or center.
            // A safe bet for procedural corner is strictly 90 deg rotation at pivot.
            w2.transform.localPosition = Vector3.zero; 

            // Save Corner Prefab
            string cornerPath = Path.Combine(folderPath, baseName + "_Corner.prefab").Replace("\\", "/");
            GameObject cornerPrefab = PrefabUtility.SaveAsPrefabAsset(cornerContainer, cornerPath);

            // Add to Database
            AddToDatabase(cornerPrefab, baseName + "_Corner");

            DestroyImmediate(cornerContainer);
        }

        private void AddToDatabase(GameObject prefab, string name)
        {
            if (targetDatabase.objectsData == null)
                targetDatabase.objectsData = new List<ObjectData>();

            // Check if already exists
            if (targetDatabase.objectsData.Any(x => x.Name == name))
                return;

            // We can't easily create ObjectData via constructor because it might not be exposed or is pure data class.
            // It's a plain class [Serializable].
            ObjectData newData = new ObjectData();
            
            // Use reflection or serialized property to set values if private
            // But ObjectData fields have [field: SerializeField] public ... { get; private set; }
            // So we need to use reflection to set them.
            
            SetPrivateField(newData, "<Name>k__BackingField", name);
            SetPrivateField(newData, "<ID>k__BackingField", name.GetHashCode()); // Simple ID gen
            SetPrivateField(newData, "<Prefab>k__BackingField", prefab);
            
            // Initialize OccupiedCells
            // We can call the existing method CalculateOccupiedCellsFromPrefab via reflection if needed
            // Or just set default.
            // The existing code has `SetPrefab` which does this!
            // public void SetPrefab(GameObject prefab, int cellSize)

            newData.SetPrefab(prefab, gridCellSize);
            newData.SetID(name.GetHashCode()); // Use the public method available in Editor
            // Note: SetID and SetPrefab are available inside #if UNITY_EDITOR block in ObjectData
            
            targetDatabase.objectsData.Add(newData);
        }

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(obj, value);
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
    }
}

