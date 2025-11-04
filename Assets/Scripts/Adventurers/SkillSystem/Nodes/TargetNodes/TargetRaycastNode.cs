using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Target Raycast node - uses raycast to find targets
/// </summary>
[System.Serializable]
public class TargetRaycastNode : SkillGraphNode
{
    private const string ORIGIN_INPUT = "origin_in";
    private const string DIRECTION_INPUT = "direction_in";
    private const string DISTANCE_INPUT = "distance_in";
    private const string HIT_OUTPUT = "hit_out";
    private const string ACTOR_OUTPUT = "actor_out";
    private const string HIT_POSITION_OUTPUT = "hit_position_out";
    
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private LayerMask layerMask = -1;
    [SerializeField] private Vector3 defaultOrigin = Vector3.zero;
    [SerializeField] private Vector3 defaultDirection = Vector3.forward;
    [SerializeField] private bool useInputOrigin = false;
    [SerializeField] private bool useInputDirection = false;
    [SerializeField] private bool useInputDistance = false;
    [SerializeField] private bool outputHitPosition = false;
    
    public TargetRaycastNode() : base()
    {
        SetTitle("Target Raycast");
        SetDescription("Uses raycast to find targets");
        maxDistance = 10f;
        layerMask = -1;
        defaultOrigin = Vector3.zero;
        defaultDirection = Vector3.forward;
        useInputOrigin = false;
        useInputDirection = false;
        useInputDistance = false;
        outputHitPosition = false;
    }
    
    public TargetRaycastNode(Vector2 pos) : base(pos)
    {
        SetTitle("Target Raycast");
        SetDescription("Uses raycast to find targets");
        maxDistance = 10f;
        layerMask = -1;
        defaultOrigin = Vector3.zero;
        defaultDirection = Vector3.forward;
        useInputOrigin = false;
        useInputDirection = false;
        useInputDistance = false;
        outputHitPosition = false;
    }
    
    public float MaxDistance
    {
        get => maxDistance;
        set
        {
            if (value < 0) value = 0;
            maxDistance = value;
        }
    }
    
    public LayerMask LayerMask
    {
        get => layerMask;
        set => layerMask = value;
    }
    
    public Vector3 DefaultOrigin
    {
        get => defaultOrigin;
        set => defaultOrigin = value;
    }
    
    public Vector3 DefaultDirection
    {
        get => defaultDirection;
        set => defaultDirection = value.normalized;
    }
    
    public bool UseInputOrigin
    {
        get => useInputOrigin;
        set
        {
            if (useInputOrigin != value)
            {
                useInputOrigin = value;
                ValidateAndCleanupConnections();
            }
        }
    }
    
    public bool UseInputDirection
    {
        get => useInputDirection;
        set
        {
            if (useInputDirection != value)
            {
                useInputDirection = value;
                ValidateAndCleanupConnections();
            }
        }
    }
    
    public bool UseInputDistance
    {
        get => useInputDistance;
        set
        {
            if (useInputDistance != value)
            {
                useInputDistance = value;
                ValidateAndCleanupConnections();
            }
        }
    }
    
    public bool OutputHitPosition
    {
        get => outputHitPosition;
        set
        {
            if (outputHitPosition != value)
            {
                outputHitPosition = value;
                ValidateAndCleanupConnections();
            }
        }
    }
    
    public override List<NodePin> GetInputPins()
    {
        var pins = new List<NodePin>();
        
        if (useInputOrigin)
        {
            pins.Add(new NodePin(ORIGIN_INPUT, "Origin", PinType.Data, PinDirection.Input, NodePinHelper.PinDataTypes.Vector3));
        }
        
        if (useInputDirection)
        {
            pins.Add(new NodePin(DIRECTION_INPUT, "Direction", PinType.Data, PinDirection.Input, NodePinHelper.PinDataTypes.Vector3));
        }
        
        if (useInputDistance)
        {
            pins.Add(new NodePin(DISTANCE_INPUT, "Distance", PinType.Data, PinDirection.Input, NodePinHelper.PinDataTypes.Float));
        }
        
        return pins;
    }
    
    public override List<NodePin> GetOutputPins()
    {
        var pins = new List<NodePin>
        {
            new NodePin(HIT_OUTPUT, "Hit", PinType.Data, PinDirection.Output, NodePinHelper.PinDataTypes.Bool),
            new NodePin(ACTOR_OUTPUT, "Actor", PinType.Data, PinDirection.Output, NodePinHelper.PinDataTypes.Actor)
        };
        
        if (outputHitPosition)
        {
            pins.Add(new NodePin(HIT_POSITION_OUTPUT, "Hit Position", PinType.Data, PinDirection.Output, NodePinHelper.PinDataTypes.Vector3));
        }
        
        return pins;
    }
}

