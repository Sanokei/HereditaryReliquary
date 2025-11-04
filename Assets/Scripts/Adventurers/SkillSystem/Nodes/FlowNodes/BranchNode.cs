using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Branch node - conditional execution (if/else)
/// </summary>
[System.Serializable]
public class BranchNode : SkillGraphNode
{
    private const string EXEC_INPUT = "exec_in";
    private const string EXEC_OUTPUT_TRUE = "exec_out_true";
    private const string EXEC_OUTPUT_FALSE = "exec_out_false";
    private const string CONDITION_INPUT = "condition_in";
    
    public BranchNode() : base()
    {
        SetTitle("Branch");
        SetDescription("Execute based on condition");
    }
    
    public BranchNode(Vector2 pos) : base(pos)
    {
        SetTitle("Branch");
        SetDescription("Execute based on condition");
    }
    
    public override List<NodePin> GetInputPins()
    {
        return new List<NodePin>
        {
            new NodePin(EXEC_INPUT, "Execute", PinType.Execution, PinDirection.Input),
            new NodePin(CONDITION_INPUT, "Condition", PinType.Data, PinDirection.Input, NodePinHelper.PinDataTypes.Bool)
        };
    }
    
    public override List<NodePin> GetOutputPins()
    {
        return new List<NodePin>
        {
            new NodePin(EXEC_OUTPUT_TRUE, "True", PinType.Execution, PinDirection.Output),
            new NodePin(EXEC_OUTPUT_FALSE, "False", PinType.Execution, PinDirection.Output)
        };
    }
}

