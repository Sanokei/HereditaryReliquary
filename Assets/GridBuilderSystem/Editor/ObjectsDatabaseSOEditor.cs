using UnityEditor;
using UnityEngine;

namespace GridBuilder.Core
{
    [CustomEditor(typeof(ObjectsDatabaseSO))]
    public class ObjectsDatabaseSOEditor : Editor
    {
        private SerializedProperty objectsDataProperty;
        private int selectedItemIndex = 0;
        
        private void OnEnable()
        {
            objectsDataProperty = serializedObject.FindProperty("objectsData");
        }
        
        public override void OnInspectorGUI()
        {
            if (target == null)
                return;
                
            ObjectsDatabaseSO database = (ObjectsDatabaseSO)target;
            
            if (database == null)
                return;
            
            serializedObject.Update();
            
            // Draw placement layer mask
            SerializedProperty placementLayerMaskProp = serializedObject.FindProperty("placementLayermask");
            if (placementLayerMaskProp != null)
            {
                EditorGUILayout.PropertyField(placementLayerMaskProp);
            }
            
            // Draw cell size
            SerializedProperty cellSizeProp = serializedObject.FindProperty("cellSize");
            if (cellSizeProp != null)
            {
                EditorGUILayout.PropertyField(cellSizeProp, new GUIContent("Cell Size", "The cell size for objects in this database."));
            }
            
            EditorGUILayout.Space();
            
            // Draw objects data list
            if (objectsDataProperty != null && objectsDataProperty.isArray)
            {
                // Show the default list inspector so users can add/remove items
                EditorGUILayout.PropertyField(objectsDataProperty, new GUIContent("Objects Data"), true);
                
                EditorGUILayout.Space();
                
                // Show edit button for selected item
                if (database.objectsData != null && database.objectsData.Count > 0)
                {
                    // Clamp selected index to valid range
                    if (selectedItemIndex >= database.objectsData.Count)
                        selectedItemIndex = 0;
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Edit Occupied Cells:", GUILayout.Width(150));
                    
                    // Show a dropdown to select which item to edit
                    // Use the actual database list to ensure we get all items
                    string[] itemNames = new string[database.objectsData.Count];
                    for (int i = 0; i < database.objectsData.Count; i++)
                    {
                        ObjectData data = database.objectsData[i];
                        if (data != null)
                        {
                            string name = !string.IsNullOrEmpty(data.Name) ? data.Name : "Unnamed";
                            string prefabName = data.Prefab != null ? data.Prefab.name : "None";
                            itemNames[i] = $"{name} ({prefabName})";
                        }
                        else
                        {
                            itemNames[i] = $"Item {i} (None)";
                        }
                    }
                    
                    selectedItemIndex = EditorGUILayout.Popup(selectedItemIndex, itemNames, GUILayout.Width(200));
                    
                    // Edit button
                    bool canEdit = false;
                    if (selectedItemIndex >= 0 && selectedItemIndex < database.objectsData.Count)
                    {
                        ObjectData selectedData = database.objectsData[selectedItemIndex];
                        canEdit = selectedData != null && selectedData.Prefab != null;
                    }
                    
                    EditorGUI.BeginDisabledGroup(!canEdit);
                    if (GUILayout.Button("Edit", GUILayout.Width(80)))
                    {
                        if (selectedItemIndex >= 0 && selectedItemIndex < database.objectsData.Count)
                        {
                            ObjectData data = database.objectsData[selectedItemIndex];
                            if (data != null && data.Prefab != null)
                            {
                                OccupiedCellsEditor.OpenWindow(data, database);
                            }
                        }
                    }
                    EditorGUI.EndDisabledGroup();
                    
                    EditorGUILayout.EndHorizontal();
                }
                else if (objectsDataProperty.arraySize == 0)
                {
                    EditorGUILayout.HelpBox("No objects in the database. Add objects using the list above.", MessageType.Info);
                }
            }
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}

