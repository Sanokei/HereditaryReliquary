using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Entry point node - where skill execution begins
/// This should be the only node without an execution input
/// </summary>
[System.Serializable]
public class EntryNode : SkillGraphNode
{
    private const string EXEC_OUTPUT = "exec_out";
    
    public EntryNode() : base()
    {
        SetTitle("Entry");
        SetDescription("Entry point for skill execution");
    }
    
    public EntryNode(Vector2 pos) : base(pos)
    {
        SetTitle("Entry");
        SetDescription("Entry point for skill execution");
    }
    
    public override List<NodePin> GetInputPins()
    {
        // Entry node has no inputs
        return new List<NodePin>();
    }
    
    public override List<NodePin> GetOutputPins()
    {
        return new List<NodePin>
        {
            new NodePin(EXEC_OUTPUT, "Execute", PinType.Execution, PinDirection.Output)
        };
    }
}

