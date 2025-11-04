using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Calculate node - performs mathematical operations
/// </summary>
[System.Serializable]
public class CalculateNode : SkillGraphNode
{
    private const string A_INPUT = "a_in";
    private const string B_INPUT = "b_in";
    private const string RESULT_OUTPUT = "result_out";
    
    [SerializeField] private CalculateOperation operation = CalculateOperation.Add;
    
    public CalculateNode() : base()
    {
        SetTitle("Calculate");
        SetDescription("Performs mathematical operations");
        operation = CalculateOperation.Add;
    }
    
    public CalculateNode(Vector2 pos) : base(pos)
    {
        SetTitle("Calculate");
        SetDescription("Performs mathematical operations");
        operation = CalculateOperation.Add;
    }
    
    public CalculateOperation Operation
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
            new NodePin(RESULT_OUTPUT, "Result", PinType.Data, PinDirection.Output, NodePinHelper.PinDataTypes.Float)
        };
    }
}

[System.Serializable]
public enum CalculateOperation
{
    Add,
    Subtract,
    Multiply,
    Divide,
    Modulo,
    Power,
    Min,
    Max
}

