using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Sequence node - executes connected nodes in order
/// </summary>
[System.Serializable]
public class SequenceNode : SkillGraphNode
{
    private const string EXEC_INPUT = "exec_in";
    private const string EXEC_OUTPUT = "exec_out";
    
    [SerializeField] private int sequenceCount = 2; // Number of output execution pins
    
    public SequenceNode() : base()
    {
        SetTitle("Sequence");
        SetDescription("Execute nodes in sequence");
        sequenceCount = 2;
    }
    
    public SequenceNode(Vector2 pos) : base(pos)
    {
        SetTitle("Sequence");
        SetDescription("Execute nodes in sequence");
        sequenceCount = 2;
    }
    
    public int SequenceCount
    {
        get => sequenceCount;
        set
        {
            if (value < 1) value = 1;
            if (value > 10) value = 10; // Limit to reasonable number
            sequenceCount = value;
        }
    }
    
    public override List<NodePin> GetInputPins()
    {
        return new List<NodePin>
        {
            new NodePin(EXEC_INPUT, "Execute", PinType.Execution, PinDirection.Input)
        };
    }
    
    public override List<NodePin> GetOutputPins()
    {
        var pins = new List<NodePin>();
        for (int i = 0; i < sequenceCount; i++)
        {
            pins.Add(new NodePin($"{EXEC_OUTPUT}_{i}", $"Then {i + 1}", PinType.Execution, PinDirection.Output));
        }
        return pins;
    }
}

