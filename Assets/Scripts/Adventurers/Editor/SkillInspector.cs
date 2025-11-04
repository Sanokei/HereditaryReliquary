using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Skill))]
public class SkillInspector : Editor
{
    private Skill skill;
    
    private void OnEnable()
    {
        skill = (Skill)target;
        
        // Ensure skill graph exists
        if (skill.GetSkillGraph() == null)
        {
            CreateSkillGraph();
        }
    }
    
    public override void OnInspectorGUI()
    {
        // Draw default inspector
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        
        // Button to open Skill Graph Editor
        if (GUILayout.Button("Open Skill Graph Editor", GUILayout.Height(30)))
        {
            OpenSkillGraphEditor();
        }
        
        // Display graph info
        var graph = skill.GetSkillGraph();
        if (graph != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Nodes: {graph.Nodes.Count}");
            EditorGUILayout.EndVertical();
        }
    }
    
    private void CreateSkillGraph()
    {
        if (skill == null) return;
        
        // Create the skill graph
        var skillGraph = ScriptableObject.CreateInstance<SkillGraph>();
        
        // Add entry node
        var entryNode = new EntryNode(Vector2.zero);
        skillGraph.AddNode(entryNode);
        skillGraph.SetEntryNode(entryNode.Id);
        
        // Assign to skill
        skill.SetSkillGraph(skillGraph);
        
        // Mark as dirty
        EditorUtility.SetDirty(skill);
        
        // If the skill is already saved as an asset, add the graph as a sub-asset
        if (AssetDatabase.Contains(skill))
        {
            skillGraph.name = skill.SkillName + "_Graph";
            AssetDatabase.AddObjectToAsset(skillGraph, skill);
            AssetDatabase.SaveAssets();
        }
    }
    
    private void OpenSkillGraphEditor()
    {
        // Open the Skill Graph Editor window with this skill
        SkillGraphEditor.OpenWithSkill(skill);
    }
}

