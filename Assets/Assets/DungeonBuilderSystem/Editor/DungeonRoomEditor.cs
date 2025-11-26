using UnityEngine;
using UnityEditor;

namespace DungeonBuilderSystem.Editor
{
    [CustomEditor(typeof(DungeonRoom))]
    public class DungeonRoomEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            DungeonRoom room = (DungeonRoom)target;

            GUILayout.Space(10);
            GUILayout.Label("Shape Generation", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Rectangle")) 
            {
                Undo.RecordObject(room.SplineContainer, "Set Shape Rectangle");
                room.SetShape(DungeonRoom.ShapeType.Rectangle);
                EditorUtility.SetDirty(room.SplineContainer);
            }
            if (GUILayout.Button("L-Shape")) 
            {
                Undo.RecordObject(room.SplineContainer, "Set Shape L");
                room.SetShape(DungeonRoom.ShapeType.L_Shape);
                EditorUtility.SetDirty(room.SplineContainer);
            }
            if (GUILayout.Button("U-Shape")) 
            {
                Undo.RecordObject(room.SplineContainer, "Set Shape U");
                room.SetShape(DungeonRoom.ShapeType.U_Shape);
                EditorUtility.SetDirty(room.SplineContainer);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("T-Shape")) 
            {
                Undo.RecordObject(room.SplineContainer, "Set Shape T");
                room.SetShape(DungeonRoom.ShapeType.T_Shape);
                EditorUtility.SetDirty(room.SplineContainer);
            }
            if (GUILayout.Button("I-Shape")) 
            {
                Undo.RecordObject(room.SplineContainer, "Set Shape I");
                room.SetShape(DungeonRoom.ShapeType.I_Shape);
                EditorUtility.SetDirty(room.SplineContainer);
            }
            GUILayout.EndHorizontal();
        }
    }
}

