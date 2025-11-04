using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Loop node - repeat execution a number of times
/// </summary>
[System.Serializable]
public class LoopNode : SkillGraphNode
{
    private const string EXEC_INPUT = "exec_in";
    private const string EXEC_OUTPUT_LOOP = "exec_out_loop";
    private const string EXEC_OUTPUT_COMPLETE = "exec_out_complete";
    private const string COUNT_INPUT = "count_in";
    private const string INDEX_OUTPUT = "index_out";
    
    [SerializeField] private int loopCount = 1;
    [SerializeField] private bool useInputCount = false;
    
    public LoopNode() : base()
    {
        SetTitle("Loop");
        SetDescription("Repeat execution");
        loopCount = 1;
        useInputCount = false;
    }
    
    public LoopNode(Vector2 pos) : base(pos)
    {
        SetTitle("Loop");
        SetDescription("Repeat execution");
        loopCount = 1;
        useInputCount = false;
    }
    
    public int LoopCount
    {
        get => loopCount;
        set
        {
            if (value < 1) value = 1;
            loopCount = value;
        }
    }
    
    public bool UseInputCount
    {
        get => useInputCount;
        set => useInputCount = value;
    }
    
    public override List<NodePin> GetInputPins()
    {
        var pins = new List<NodePin>
        {
            new NodePin(EXEC_INPUT, "Execute", PinType.Execution, PinDirection.Input)
        };
        
        if (useInputCount)
        {
            pins.Add(new NodePin(COUNT_INPUT, "Count", PinType.Data, PinDirection.Input, NodePinHelper.PinDataTypes.Int));
        }
        
        return pins;
    }
    
    public override List<NodePin> GetOutputPins()
    {
        var pins = new List<NodePin>
        {
            new NodePin(EXEC_OUTPUT_LOOP, "Loop", PinType.Execution, PinDirection.Output),
            new NodePin(EXEC_OUTPUT_COMPLETE, "Complete", PinType.Execution, PinDirection.Output),
            new NodePin(INDEX_OUTPUT, "Index", PinType.Data, PinDirection.Output, NodePinHelper.PinDataTypes.Int)
        };
        
        return pins;
    }
}

