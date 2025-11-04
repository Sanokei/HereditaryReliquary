using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Delay node - wait for a specified time before continuing
/// </summary>
[System.Serializable]
public class DelayNode : SkillGraphNode
{
    private const string EXEC_INPUT = "exec_in";
    private const string EXEC_OUTPUT = "exec_out";
    private const string DURATION_INPUT = "duration_in";
    
    [SerializeField] private float delayDuration = 1.0f;
    [SerializeField] private bool useInputDuration = false;
    
    public DelayNode() : base()
    {
        SetTitle("Delay");
        SetDescription("Wait for specified time");
        delayDuration = 1.0f;
        useInputDuration = false;
    }
    
    public DelayNode(Vector2 pos) : base(pos)
    {
        SetTitle("Delay");
        SetDescription("Wait for specified time");
        delayDuration = 1.0f;
        useInputDuration = false;
    }
    
    public float DelayDuration
    {
        get => delayDuration;
        set
        {
            if (value < 0) value = 0;
            delayDuration = value;
        }
    }
    
    public bool UseInputDuration
    {
        get => useInputDuration;
        set => useInputDuration = value;
    }
    
    public override List<NodePin> GetInputPins()
    {
        var pins = new List<NodePin>
        {
            new NodePin(EXEC_INPUT, "Execute", PinType.Execution, PinDirection.Input)
        };
        
        if (useInputDuration)
        {
            pins.Add(new NodePin(DURATION_INPUT, "Duration", PinType.Data, PinDirection.Input, NodePinHelper.PinDataTypes.Float));
        }
        
        return pins;
    }
    
    public override List<NodePin> GetOutputPins()
    {
        return new List<NodePin>
        {
            new NodePin(EXEC_OUTPUT, "Complete", PinType.Execution, PinDirection.Output)
        };
    }
}

