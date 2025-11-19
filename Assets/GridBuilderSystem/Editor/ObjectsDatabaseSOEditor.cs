using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace GridBuilder.Core
{
    [CustomEditor(typeof(ObjectsDatabaseSO))]
    public class ObjectsDatabaseSOEditor : Editor
    {
        private SerializedProperty objectsDataProperty;
        private SerializedProperty placementLayerMaskProp;
        private SerializedProperty cellSizeProp;
        private ReorderableList reorderableList;
        
        private void OnEnable()
        {
            objectsDataProperty = serializedObject.FindProperty("objectsData");
            placementLayerMaskProp = serializedObject.FindProperty("placementLayermask");
            cellSizeProp = serializedObject.FindProperty("cellSize");
            
            // Create reorderable list
            reorderableList = new ReorderableList(serializedObject, objectsDataProperty, true, true, true, true);
            reorderableList.drawHeaderCallback = (Rect rect) => {
                EditorGUI.LabelField(rect, "Objects Data");
            };
            reorderableList.drawElementCallback = DrawElement;
            reorderableList.elementHeightCallback = GetElementHeight;
            reorderableList.onAddCallback = OnAddElement;
            reorderableList.onRemoveCallback = OnRemoveElement;
            reorderableList.onReorderCallbackWithDetails = OnReorderElement;
        }
        
        private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index >= objectsDataProperty.arraySize)
                return;
                
            SerializedProperty element = objectsDataProperty.GetArrayElementAtIndex(index);
            if (element == null)
                return;
                
            ObjectsDatabaseSO database = (ObjectsDatabaseSO)target;
            
            float yOffset = 0f;
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = 2f;
            
            // Draw ID (read-only, shows index)
            Rect idRect = new Rect(rect.x, rect.y + yOffset, rect.width, lineHeight);
            EditorGUI.LabelField(idRect, $"ID: {index}", EditorStyles.boldLabel);
            yOffset += lineHeight + spacing;
            
            // Update ID in ObjectData if it doesn't match index
            if (database.objectsData != null && index < database.objectsData.Count)
            {
                ObjectData objData = database.objectsData[index];
                if (objData != null && objData.ID != index)
                {
                    objData.SetID(index);
                    EditorUtility.SetDirty(database);
                }
            }
            
            // Draw all properties
            // Iterate through all properties to find the correct names
            SerializedProperty nameProp = null;
            SerializedProperty prefabProp = null;
            SerializedProperty occupiedCellsProp = null;
            SerializedProperty validatorsProp = null;
            
            SerializedProperty iterator = element.Copy();
            SerializedProperty endProperty = element.GetEndProperty();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, endProperty))
            {
                enterChildren = false;
                string propName = iterator.name;
                
                if (propName == "Name" || propName == "name" || propName.Contains("Name"))
                    nameProp = iterator.Copy();
                else if (propName == "Prefab" || propName == "prefab" || propName.Contains("Prefab"))
                    prefabProp = iterator.Copy();
                else if (propName == "OccupiedCells" || propName == "occupiedCells" || propName.Contains("OccupiedCells"))
                    occupiedCellsProp = iterator.Copy();
                else if (propName == "placementValidators")
                    validatorsProp = iterator.Copy();
            }
            
            // Fallback: try direct property access
            if (nameProp == null)
                nameProp = element.FindPropertyRelative("Name");
            if (prefabProp == null)
                prefabProp = element.FindPropertyRelative("Prefab");
            if (occupiedCellsProp == null)
                occupiedCellsProp = element.FindPropertyRelative("OccupiedCells");
            if (validatorsProp == null)
                validatorsProp = element.FindPropertyRelative("placementValidators");
            
            // Name
            Rect nameRect = new Rect(rect.x, rect.y + yOffset, rect.width, lineHeight);
            if (nameProp != null)
            {
                EditorGUI.PropertyField(nameRect, nameProp);
            }
            else
            {
                EditorGUI.LabelField(nameRect, "Name: (property not found)");
            }
            yOffset += lineHeight + spacing;
            
            // Prefab with auto-calculate
            Rect prefabRect = new Rect(rect.x, rect.y + yOffset, rect.width, lineHeight);
            if (prefabProp != null)
            {
                GameObject oldPrefab = prefabProp.objectReferenceValue as GameObject;
                EditorGUI.PropertyField(prefabRect, prefabProp);
                GameObject newPrefab = prefabProp.objectReferenceValue as GameObject;
                
                // Auto-calculate occupied cells when prefab changes
                if (newPrefab != oldPrefab && newPrefab != null && database != null)
                {
                    if (index < database.objectsData.Count)
                    {
                        ObjectData objData = database.objectsData[index];
                        if (objData != null)
                        {
                            objData.SetPrefab(newPrefab, database.CellSize);
                            EditorUtility.SetDirty(database);
                            serializedObject.Update();
                        }
                    }
                }
            }
            else
            {
                EditorGUI.LabelField(prefabRect, "Prefab: (property not found)");
            }
            yOffset += lineHeight + spacing;
            
            // Occupied Cells (read-only display, use Edit button to modify)
            Rect occupiedRect = new Rect(rect.x, rect.y + yOffset, rect.width, lineHeight);
            int cellCount = 0;
            if (occupiedCellsProp != null && occupiedCellsProp.isArray)
            {
                cellCount = occupiedCellsProp.arraySize;
            }
            EditorGUI.LabelField(occupiedRect, $"Occupied Cells: {cellCount} cells");
            yOffset += lineHeight + spacing;
            
            // Edit Occupied Cells button (show if prefab exists)
            if (prefabProp != null)
            {
                GameObject prefab = prefabProp.objectReferenceValue as GameObject;
                if (prefab != null)
                {
                    Rect editRect = new Rect(rect.x, rect.y + yOffset, rect.width, lineHeight);
                    if (GUI.Button(editRect, "Edit Occupied Cells"))
                    {
                        if (index < database.objectsData.Count)
                        {
                            ObjectData objData = database.objectsData[index];
                            if (objData != null)
                            {
                                OccupiedCellsEditor.OpenWindow(objData, database);
                            }
                        }
                    }
                    yOffset += lineHeight + spacing;
                }
            }
            
            // Validators
            if (validatorsProp != null)
            {
                Rect validatorsRect = new Rect(rect.x, rect.y + yOffset, rect.width, EditorGUI.GetPropertyHeight(validatorsProp, true));
                EditorGUI.PropertyField(validatorsRect, validatorsProp, true);
                yOffset += validatorsRect.height + spacing;
            }
        }
        
        private float GetElementHeight(int index)
        {
            // Return minimum height if properties aren't available
            if (objectsDataProperty == null || index < 0 || index >= objectsDataProperty.arraySize)
                return EditorGUIUtility.singleLineHeight + 4f;
                
            SerializedProperty element = objectsDataProperty.GetArrayElementAtIndex(index);
            if (element == null)
                return EditorGUIUtility.singleLineHeight + 4f;
            
            float height = EditorGUIUtility.singleLineHeight; // ID
            float spacing = 2f;
            
            // Always show all properties
            height += EditorGUIUtility.singleLineHeight + spacing; // Name
            height += EditorGUIUtility.singleLineHeight + spacing; // Prefab
            height += EditorGUIUtility.singleLineHeight + spacing; // Occupied Cells label
            
            try
            {
                // Find properties using same method as DrawElement
                SerializedProperty prefabProp = null;
                SerializedProperty validatorsProp = null;
                
                SerializedProperty iterator = element.Copy();
                SerializedProperty endProperty = element.GetEndProperty();
                bool enterChildren = true;
                while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, endProperty))
                {
                    enterChildren = false;
                    string propName = iterator.name;
                    
                    if (propName == "Prefab" || propName == "prefab" || propName.Contains("Prefab"))
                        prefabProp = iterator.Copy();
                    else if (propName == "placementValidators")
                        validatorsProp = iterator.Copy();
                }
                
                // Fallback
                if (prefabProp == null)
                    prefabProp = element.FindPropertyRelative("Prefab");
                if (validatorsProp == null)
                    validatorsProp = element.FindPropertyRelative("placementValidators");
                
                if (prefabProp != null && prefabProp.objectReferenceValue != null)
                {
                    height += EditorGUIUtility.singleLineHeight + spacing; // Edit button
                }
                
                if (validatorsProp != null)
                {
                    height += EditorGUI.GetPropertyHeight(validatorsProp, true) + spacing;
                }
            }
            catch
            {
                // If property access fails, return current height
                // This prevents layout errors
            }
            
            return height + 4f; // Add padding
        }
        
        private void OnAddElement(ReorderableList list)
        {
            int index = list.index >= 0 ? list.index : list.count;
            objectsDataProperty.arraySize++;
            list.index = index;
            
            // Apply changes to ensure properties are available
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
            
            // Create a blank ObjectData entry
            SerializedProperty newElement = objectsDataProperty.GetArrayElementAtIndex(index);
            if (newElement != null)
            {
                SerializedProperty nameProp = newElement.FindPropertyRelative("Name");
                SerializedProperty prefabProp = newElement.FindPropertyRelative("Prefab");
                SerializedProperty occupiedCellsProp = newElement.FindPropertyRelative("OccupiedCells");
                
                if (nameProp != null)
                    nameProp.stringValue = "";
                if (prefabProp != null)
                    prefabProp.objectReferenceValue = null;
                if (occupiedCellsProp != null)
                {
                    occupiedCellsProp.ClearArray();
                    occupiedCellsProp.arraySize = 1;
                    var firstCell = occupiedCellsProp.GetArrayElementAtIndex(0);
                    if (firstCell != null)
                    {
                        firstCell.vector3IntValue = Vector3Int.zero;
                    }
                }
            }
            
            // Update IDs for all elements
            UpdateAllIDs();
            
            // Apply changes
            serializedObject.ApplyModifiedProperties();
        }
        
        private void OnRemoveElement(ReorderableList list)
        {
            objectsDataProperty.DeleteArrayElementAtIndex(list.index);
            UpdateAllIDs();
        }
        
        private void OnReorderElement(ReorderableList list, int oldIndex, int newIndex)
        {
            objectsDataProperty.MoveArrayElement(oldIndex, newIndex);
            UpdateAllIDs();
        }
        
        private void UpdateAllIDs()
        {
            ObjectsDatabaseSO database = (ObjectsDatabaseSO)target;
            if (database.objectsData != null)
            {
                for (int i = 0; i < database.objectsData.Count; i++)
                {
                    if (database.objectsData[i] != null)
                    {
                        database.objectsData[i].SetID(i);
                    }
                }
                EditorUtility.SetDirty(database);
            }
        }
        
        public override void OnInspectorGUI()
        {
            if (target == null)
                return;
                
            ObjectsDatabaseSO database = (ObjectsDatabaseSO)target;
            
            if (database == null)
                return;
            
            serializedObject.Update();
            
            // Ensure properties are valid
            if (objectsDataProperty == null)
            {
                objectsDataProperty = serializedObject.FindProperty("objectsData");
            }
            
            // Draw placement layer mask
            if (placementLayerMaskProp != null)
            {
                EditorGUILayout.PropertyField(placementLayerMaskProp);
            }
            
            // Draw cell size
            if (cellSizeProp != null)
            {
                EditorGUILayout.PropertyField(cellSizeProp, new GUIContent("Cell Size", "The cell size for objects in this database."));
            }
            
            EditorGUILayout.Space();
            
            // Draw reorderable list
            if (reorderableList != null && objectsDataProperty != null)
            {
                reorderableList.DoLayoutList();
            }
            
            // Update IDs after any changes
            UpdateAllIDs();
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}

