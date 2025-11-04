using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Move node - moves an actor to a target position
/// </summary>
[System.Serializable]
public class MoveNode : SkillGraphNode
{
    private const string EXEC_INPUT = "exec_in";
    private const string EXEC_OUTPUT = "exec_out";
    private const string ACTOR_INPUT = "actor_in";
    private const string TARGET_POSITION_INPUT = "target_position_in";
    private const string SPEED_INPUT = "speed_in";
    
    [SerializeField] private MovementType movementType = MovementType.Lerp;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private bool useInputSpeed = false;
    [SerializeField] private bool useInputPosition = true;
    [SerializeField] private Vector3 defaultPosition = Vector3.zero;
    [SerializeField] private bool waitForCompletion = true;
    [SerializeField] private float lerpSpeed = 2f;
    
    public MoveNode() : base()
    {
        SetTitle("Move");
        SetDescription("Moves an actor to target position");
        movementType = MovementType.Lerp;
        moveSpeed = 5f;
        useInputSpeed = false;
        useInputPosition = true;
        defaultPosition = Vector3.zero;
        waitForCompletion = true;
        lerpSpeed = 2f;
    }
    
    public MoveNode(Vector2 pos) : base(pos)
    {
        SetTitle("Move");
        SetDescription("Moves an actor to target position");
        movementType = MovementType.Lerp;
        moveSpeed = 5f;
        useInputSpeed = false;
        useInputPosition = true;
        defaultPosition = Vector3.zero;
        waitForCompletion = true;
        lerpSpeed = 2f;
    }
    
    public MovementType MovementType
    {
        get => movementType;
        set => movementType = value;
    }
    
    public float MoveSpeed
    {
        get => moveSpeed;
        set
        {
            if (value < 0) value = 0;
            moveSpeed = value;
        }
    }
    
    public bool UseInputSpeed
    {
        get => useInputSpeed;
        set
        {
            if (useInputSpeed != value)
            {
                useInputSpeed = value;
                ValidateAndCleanupConnections();
            }
        }
    }
    
    public bool UseInputPosition
    {
        get => useInputPosition;
        set
        {
            if (useInputPosition != value)
            {
                useInputPosition = value;
                ValidateAndCleanupConnections();
            }
        }
    }
    
    public Vector3 DefaultPosition
    {
        get => defaultPosition;
        set => defaultPosition = value;
    }
    
    public bool WaitForCompletion
    {
        get => waitForCompletion;
        set => waitForCompletion = value;
    }
    
    public float LerpSpeed
    {
        get => lerpSpeed;
        set
        {
            if (value < 0) value = 0;
            lerpSpeed = value;
        }
    }
    
    public override List<NodePin> GetInputPins()
    {
        var pins = new List<NodePin>
        {
            new NodePin(EXEC_INPUT, "Execute", PinType.Execution, PinDirection.Input),
            new NodePin(ACTOR_INPUT, "Actor", PinType.Data, PinDirection.Input, NodePinHelper.PinDataTypes.Actor)
        };
        
        if (useInputPosition)
        {
            pins.Add(new NodePin(TARGET_POSITION_INPUT, "Target Position", PinType.Data, PinDirection.Input, NodePinHelper.PinDataTypes.Vector3));
        }
        
        if (useInputSpeed)
        {
            pins.Add(new NodePin(SPEED_INPUT, "Speed", PinType.Data, PinDirection.Input, NodePinHelper.PinDataTypes.Float));
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

[System.Serializable]
public enum MovementType
{
    Instant,
    Lerp,
    SmoothDamp
}

