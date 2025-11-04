using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Skill))]
public class SkillInspector : Editor
{
    private Skill skill;
    
    private void OnEnable()
    {
        skill = (Skill)target;
    }
    
    public override void OnInspectorGUI()
    {
        // Draw default inspector
        DrawDefaultInspector();
    }
}

