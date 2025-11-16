#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

namespace GridBuilder.Core
{
    /// <summary>
    /// Utility to generate thumbnail sprites for dungeon objects
    /// </summary>
    public class DungeonThumbnailGenerator : EditorWindow
    {
        private DungeonObjectsDatabaseSO database;
        
        [MenuItem("Tools/GridBuilder/Generate Dungeon Thumbnails")]
        static void ShowWindow()
        {
            var window = GetWindow<DungeonThumbnailGenerator>("Thumbnail Generator");
            window.minSize = new Vector2(300, 150);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Generate Thumbnails for Dungeon Objects", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            database = (DungeonObjectsDatabaseSO)EditorGUILayout.ObjectField(
                "Dungeon Database", database, typeof(DungeonObjectsDatabaseSO), false);

            EditorGUILayout.Space();

            GUI.enabled = database != null && database.ObjectsDatabase != null;
            if (GUILayout.Button("Generate All Thumbnails", GUILayout.Height(30)))
            {
                GenerateAllThumbnails();
            }
            GUI.enabled = true;

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "This will create thumbnail sprites for all objects in the database. " +
                "Thumbnails will be saved in the Thumbnails folder next to the database asset.",
                MessageType.Info);
        }

        private void GenerateAllThumbnails()
        {
            if (database == null || database.ObjectsDatabase == null)
            {
                Debug.LogError("Invalid database selected.");
                return;
            }

            // Save current scene
            string originalScenePath = EditorSceneManager.GetActiveScene().path;
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            // Create temporary scene
            Scene tempScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            try
            {
                var allObjects = database.GetAllObjects();
                int count = 0;

                foreach (var objectData in allObjects)
                {
                    if (objectData != null && objectData.Prefab != null)
                    {
                        GenerateThumbnailForObject(objectData, tempScene);
                        count++;
                    }
                }

                Debug.Log($"Generated {count} thumbnails successfully.");
            }
            finally
            {
                // Restore original scene
                if (!string.IsNullOrEmpty(originalScenePath))
                {
                    EditorSceneManager.OpenScene(originalScenePath);
                }
                else
                {
                    EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
                }

                EditorUtility.SetDirty(database);
                AssetDatabase.SaveAssets();
            }
        }

        private void GenerateThumbnailForObject(ObjectData objectData, Scene targetScene)
        {
            EditorSceneManager.SetActiveScene(targetScene);

            // Instantiate prefab
            GameObject instance = PrefabUtility.InstantiatePrefab(objectData.Prefab) as GameObject;
            if (instance == null)
            {
                Debug.LogError($"Failed to instantiate prefab: {objectData.Name}");
                return;
            }

            // Create camera
            var cameraGO = new GameObject("ThumbnailCamera", typeof(Camera));
            var camera = cameraGO.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 2f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(82, 82, 82, 255);

            // Position camera
            cameraGO.transform.position = instance.transform.position + new Vector3(2, 3, -2);
            cameraGO.transform.LookAt(instance.transform);

            // Create lighting
            var lightGO = new GameObject("ThumbnailLight");
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
            light.intensity = 1f;

            // Render texture
            var rt = new RenderTexture(256, 256, 24);
            camera.targetTexture = rt;
            camera.Render();

            // Create texture
            var tex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
            tex.Apply();

            // Save texture
            var path = AssetDatabase.GetAssetPath(database);
            var dir = Path.GetDirectoryName(path);
            var thumbDir = Path.Combine(dir, "Thumbnails");

            if (!AssetDatabase.IsValidFolder(thumbDir))
            {
                var parentDir = Path.GetDirectoryName(thumbDir);
                var folderName = Path.GetFileName(thumbDir);
                AssetDatabase.CreateFolder(parentDir, folderName);
            }

            var texPath = Path.Combine(thumbDir, $"{objectData.Name}_thumb.png");
            File.WriteAllBytes(texPath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(texPath);

            // Set texture import settings
            var textureImporter = AssetImporter.GetAtPath(texPath) as TextureImporter;
            if (textureImporter != null)
            {
                textureImporter.textureType = TextureImporterType.Sprite;
                textureImporter.spriteImportMode = SpriteImportMode.Single;
                textureImporter.spritePixelsPerUnit = 100;
                AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);
            }

            // Load and assign sprite
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texPath);
            if (sprite != null)
            {
                database.SetThumbnail(objectData.ID, sprite);
            }

            // Cleanup
            RenderTexture.active = null;
            DestroyImmediate(cameraGO);
            DestroyImmediate(rt);
            DestroyImmediate(instance);
            DestroyImmediate(lightGO);
        }
    }
}
#endif

