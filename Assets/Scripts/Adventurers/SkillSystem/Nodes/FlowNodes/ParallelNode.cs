using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Parallel node - execute multiple branches simultaneously
/// </summary>
[System.Serializable]
public class ParallelNode : SkillGraphNode
{
    private const string EXEC_INPUT = "exec_in";
    private const string EXEC_OUTPUT = "exec_out";
    
    [SerializeField] private int branchCount = 2; // Number of parallel branches
    
    public ParallelNode() : base()
    {
        SetTitle("Parallel");
        SetDescription("Execute branches simultaneously");
        branchCount = 2;
    }
    
    public ParallelNode(Vector2 pos) : base(pos)
    {
        SetTitle("Parallel");
        SetDescription("Execute branches simultaneously");
        branchCount = 2;
    }
    
    public int BranchCount
    {
        get => branchCount;
        set
        {
            if (value < 1) value = 1;
            if (value > 10) value = 10; // Limit to reasonable number
            branchCount = value;
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
        var pins = new List<NodePin>
        {
            new NodePin(EXEC_OUTPUT, "Complete", PinType.Execution, PinDirection.Output)
        };
        
        for (int i = 0; i < branchCount; i++)
        {
            pins.Add(new NodePin($"exec_out_{i}", $"Branch {i + 1}", PinType.Execution, PinDirection.Output));
        }
        
        return pins;
    }
}

