using UnityEditor;
using UnityEngine;

namespace GridBuilder.Core
{
    [CustomEditor(typeof(SplineGridContainer))]
    [CanEditMultipleObjects]
    public class SplineGridContainerEditor : Editor
    {
        private SerializedProperty gridMaterialProp;
        private SerializedProperty gridCellSizeProp;
        private SerializedProperty gridSizeProp;
        private SerializedProperty placementLayerMaskProp;
        private SerializedProperty placementModeProp;
        
        // Ring settings
        private SerializedProperty ringPrefabProp;
        private SerializedProperty ringParentProp;
        private SerializedProperty ringPositionOffsetProp;
        private SerializedProperty autoCreateRingOnStartProp;
        private SerializedProperty clearExistingRingOnCreateProp;
        
        private bool showRingSettings = true;
        
        private void OnEnable()
        {
            // Standard properties
            gridMaterialProp = serializedObject.FindProperty("gridMaterial");
            gridCellSizeProp = serializedObject.FindProperty("gridCellSize");
            gridSizeProp = serializedObject.FindProperty("gridSize");
            placementLayerMaskProp = serializedObject.FindProperty("placementLayerMask");
            placementModeProp = serializedObject.FindProperty("placementMode");
            
            // Ring properties
            ringPrefabProp = serializedObject.FindProperty("ringPrefab");
            ringParentProp = serializedObject.FindProperty("ringParent");
            ringPositionOffsetProp = serializedObject.FindProperty("ringPositionOffset");
            autoCreateRingOnStartProp = serializedObject.FindProperty("autoCreateRingOnStart");
            clearExistingRingOnCreateProp = serializedObject.FindProperty("clearExistingRingOnCreate");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            // Draw default properties
            EditorGUILayout.PropertyField(gridMaterialProp);
            EditorGUILayout.PropertyField(gridCellSizeProp);
            EditorGUILayout.PropertyField(gridSizeProp);
            EditorGUILayout.PropertyField(placementLayerMaskProp);
            EditorGUILayout.PropertyField(placementModeProp);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            
            // Ring settings foldout
            showRingSettings = EditorGUILayout.Foldout(showRingSettings, "Outside Ring Settings", true);
            if (showRingSettings)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(ringPrefabProp, new GUIContent("Ring Prefab", "Prefab to instantiate at each ring cell position"));
                EditorGUILayout.PropertyField(ringParentProp, new GUIContent("Ring Parent", "Parent transform for ring objects. If null, uses this transform."));
                EditorGUILayout.PropertyField(ringPositionOffsetProp, new GUIContent("Position Offset", "Offset to apply to each ring object's position"));
                EditorGUILayout.PropertyField(autoCreateRingOnStartProp, new GUIContent("Auto Create On Start", "Automatically create the ring when the scene starts"));
                EditorGUILayout.PropertyField(clearExistingRingOnCreateProp, new GUIContent("Clear Existing On Create", "Clear existing ring objects before creating new ones"));
                
                EditorGUI.indentLevel--;
                
                EditorGUILayout.Space();
                
                // Create ring button
                EditorGUI.BeginDisabledGroup(ringPrefabProp.objectReferenceValue == null);
                
                if (GUILayout.Button("Create Ring Now", GUILayout.Height(30)))
                {
                    CreateRingForTargets();
                }
                
                EditorGUI.EndDisabledGroup();
                
                if (ringPrefabProp.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Please assign a Ring Prefab to create the ring.", MessageType.Warning);
                }
                else if (!Application.isPlaying)
                {
                    EditorGUILayout.HelpBox("Ring will be created in the scene. Use 'Auto Create On Start' to create rings automatically when entering Play Mode.", MessageType.Info);
                }
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void CreateRingForTargets()
        {
            foreach (Object target in targets)
            {
                SplineGridContainer container = target as SplineGridContainer;
                if (container == null)
                    continue;
                
                GameObject prefab = ringPrefabProp.objectReferenceValue as GameObject;
                Transform parent = ringParentProp.objectReferenceValue as Transform;
                Vector3 offset = ringPositionOffsetProp.vector3Value;
                bool clearExisting = clearExistingRingOnCreateProp.boolValue;
                
                container.CreateOutsideRing(prefab, parent, offset, clearExisting);
            }
        }
    }
}

