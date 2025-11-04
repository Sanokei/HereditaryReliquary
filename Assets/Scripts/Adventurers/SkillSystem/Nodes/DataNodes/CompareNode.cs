using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Compare node - compares two values and outputs a boolean
/// </summary>
[System.Serializable]
public class CompareNode : SkillGraphNode
{
    private const string A_INPUT = "a_in";
    private const string B_INPUT = "b_in";
    private const string RESULT_OUTPUT = "result_out";
    
    [SerializeField] private CompareOperation operation = CompareOperation.Equal;
    
    public CompareNode() : base()
    {
        SetTitle("Compare");
        SetDescription("Compares two values");
        operation = CompareOperation.Equal;
    }
    
    public CompareNode(Vector2 pos) : base(pos)
    {
        SetTitle("Compare");
        SetDescription("Compares two values");
        operation = CompareOperation.Equal;
    }
    
    public CompareOperation Operation
    {
        get => operation;
        set => operation = value;
    }
    
    public override List<NodePin> GetInputPins()
    {
        return new List<NodePin>
        {
            new NodePin(A_INPUT, "A", PinType.Data, PinDirection.Input, NodePinHelper.PinDataTypes.Float),
            new NodePin(B_INPUT, "B", PinType.Data, PinDirection.Input, NodePinHelper.PinDataTypes.Float)
        };
    }
    
    public override List<NodePin> GetOutputPins()
    {
        return new List<NodePin>
        {
            new NodePin(RESULT_OUTPUT, "Result", PinType.Data, PinDirection.Output, NodePinHelper.PinDataTypes.Bool)
        };
    }
}

[System.Serializable]
public enum CompareOperation
{
    Equal,
    NotEqual,
    GreaterThan,
    LessThan,
    GreaterThanOrEqual,
    LessThanOrEqual
}

