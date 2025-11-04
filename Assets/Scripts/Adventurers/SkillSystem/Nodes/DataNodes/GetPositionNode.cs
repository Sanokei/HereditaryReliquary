using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Get Position node - gets the position of an actor or transform
/// </summary>
[System.Serializable]
public class GetPositionNode : SkillGraphNode
{
    private const string ACTOR_INPUT = "actor_in";
    private const string TRANSFORM_INPUT = "transform_in";
    private const string POSITION_OUTPUT = "position_out";
    private const string FORWARD_OUTPUT = "forward_out";
    private const string RIGHT_OUTPUT = "right_out";
    private const string UP_OUTPUT = "up_out";
    
    [SerializeField] private bool useTransformInput = false;
    [SerializeField] private bool outputForward = false;
    [SerializeField] private bool outputRight = false;
    [SerializeField] private bool outputUp = false;
    
    public GetPositionNode() : base()
    {
        SetTitle("Get Position");
        SetDescription("Gets the position of an actor or transform");
        useTransformInput = false;
        outputForward = false;
        outputRight = false;
        outputUp = false;
    }
    
    public GetPositionNode(Vector2 pos) : base(pos)
    {
        SetTitle("Get Position");
        SetDescription("Gets the position of an actor or transform");
        useTransformInput = false;
        outputForward = false;
        outputRight = false;
        outputUp = false;
    }
    
    public bool UseTransformInput
    {
        get => useTransformInput;
        set
        {
            if (useTransformInput != value)
            {
                useTransformInput = value;
                ValidateAndCleanupConnections();
            }
        }
    }
    
    public bool OutputForward
    {
        get => outputForward;
        set
        {
            if (outputForward != value)
            {
                outputForward = value;
                ValidateAndCleanupConnections();
            }
        }
    }
    
    public bool OutputRight
    {
        get => outputRight;
        set
        {
            if (outputRight != value)
            {
                outputRight = value;
                ValidateAndCleanupConnections();
            }
        }
    }
    
    public bool OutputUp
    {
        get => outputUp;
        set
        {
            if (outputUp != value)
            {
                outputUp = value;
                ValidateAndCleanupConnections();
            }
        }
    }
    
    public override List<NodePin> GetInputPins()
    {
        var pins = new List<NodePin>();
        
        if (useTransformInput)
        {
            pins.Add(new NodePin(TRANSFORM_INPUT, "Transform", PinType.Data, PinDirection.Input, NodePinHelper.PinDataTypes.Transform));
        }
        else
        {
            pins.Add(new NodePin(ACTOR_INPUT, "Actor", PinType.Data, PinDirection.Input, NodePinHelper.PinDataTypes.Actor));
        }
        
        return pins;
    }
    
    public override List<NodePin> GetOutputPins()
    {
        var pins = new List<NodePin>
        {
            new NodePin(POSITION_OUTPUT, "Position", PinType.Data, PinDirection.Output, NodePinHelper.PinDataTypes.Vector3)
        };
        
        if (outputForward)
        {
            pins.Add(new NodePin(FORWARD_OUTPUT, "Forward", PinType.Data, PinDirection.Output, NodePinHelper.PinDataTypes.Vector3));
        }
        
        if (outputRight)
        {
            pins.Add(new NodePin(RIGHT_OUTPUT, "Right", PinType.Data, PinDirection.Output, NodePinHelper.PinDataTypes.Vector3));
        }
        
        if (outputUp)
        {
            pins.Add(new NodePin(UP_OUTPUT, "Up", PinType.Data, PinDirection.Output, NodePinHelper.PinDataTypes.Vector3));
        }
        
        return pins;
    }
}

