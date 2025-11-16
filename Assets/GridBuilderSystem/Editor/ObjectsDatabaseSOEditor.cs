using UnityEditor;
using UnityEngine;

namespace GridBuilder.Core
{
    [CustomEditor(typeof(ObjectsDatabaseSO))]
    public class ObjectsDatabaseSOEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            ObjectsDatabaseSO database = (ObjectsDatabaseSO)target;
            
            // Draw default inspector
            DrawDefaultInspector();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Object Data Editor", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            if (database.objectsData != null)
            {
                for (int i = 0; i < database.objectsData.Count; i++)
                {
                    ObjectData data = database.objectsData[i];
                    
                    if (data == null)
                        continue;
                    
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    EditorGUILayout.LabelField($"{data.Name} (ID: {data.ID})", EditorStyles.boldLabel);
                    
                    if (data.Prefab != null)
                    {
                        EditorGUILayout.LabelField($"Prefab: {data.Prefab.name}");
                        EditorGUILayout.LabelField($"Occupied Cells: {(data.OccupiedCells != null ? data.OccupiedCells.Count : 0)}");
                        
                        if (GUILayout.Button($"Edit Occupied Cells for {data.Name}"))
                        {
                            OccupiedCellsEditor.OpenWindow(data, database);
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("No prefab assigned", MessageType.Warning);
                    }
                    
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space();
                }
            }
        }
    }
}

