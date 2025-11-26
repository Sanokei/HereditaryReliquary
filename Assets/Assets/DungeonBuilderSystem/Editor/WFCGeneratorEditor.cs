using UnityEngine;
using UnityEditor;

namespace DungeonBuilderSystem.Editor
{
    [CustomEditor(typeof(WFCGenerator))]
    public class WFCGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            WFCGenerator generator = (WFCGenerator)target;

            GUILayout.Space(10);
            
            if (GUILayout.Button("Generate Dungeon Room"))
            {
                // Ensure we can undo
                Undo.RegisterCompleteObjectUndo(generator.gameObject, "Generate Dungeon");
                
                // We might need to register undo for the room's children if they are destroyed/created
                // But simpler to just let the script handle it.
                // For full undo support, the generator script should use Undo.DestroyObjectImmediate etc.
                // But for now, a button is sufficient.
                
                generator.Generate();
                
                // Mark scene dirty
                if (!Application.isPlaying)
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
                }
            }

            if (GUILayout.Button("Clear Dungeon"))
            {
                 // Ideally WFCGenerator should have a Clear method or we access room directly
                 // But room.InitializeForGeneration() effectively clears.
                 // Let's assume Generate() calls it.
                 // We can add a public Clear() to WFCGenerator if we want separate control.
                 // For now, I'll just rely on Generate to clear and regenerate.
                 
                 // Just finding the DungeonRoom component on the generator's object or referenced one
                 // The generator references 'room'.
                 
                 SerializedProperty roomProp = serializedObject.FindProperty("room");
                 if (roomProp.objectReferenceValue != null)
                 {
                     DungeonRoom room = (DungeonRoom)roomProp.objectReferenceValue;
                     Undo.RegisterFullObjectHierarchyUndo(room.gameObject, "Clear Dungeon");
                     room.InitializeForGeneration();
                     if (!Application.isPlaying)
                        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(room.gameObject.scene);
                 }
            }
        }
    }
}

