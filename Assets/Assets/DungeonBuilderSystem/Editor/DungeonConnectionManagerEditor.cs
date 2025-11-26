using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace DungeonBuilderSystem.Editor
{
    [CustomEditor(typeof(DungeonConnectionManager))]
    public class DungeonConnectionManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            DungeonConnectionManager manager = (DungeonConnectionManager)target;

            GUILayout.Space(10);
            
            if (GUILayout.Button("Connect All Rooms in Scene"))
            {
                // Find all DungeonRooms
                DungeonRoom[] rooms = FindObjectsOfType<DungeonRoom>();
                if (rooms.Length < 2)
                {
                    Debug.LogWarning("Need at least 2 DungeonRooms to connect.");
                    return;
                }
                
                Undo.RegisterCompleteObjectUndo(rooms, "Connect Dungeon Rooms");
                manager.ConnectRooms(new List<DungeonRoom>(rooms));
            }
        }
    }
}

