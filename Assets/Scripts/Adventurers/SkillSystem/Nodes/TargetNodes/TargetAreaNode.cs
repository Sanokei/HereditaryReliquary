using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Target Area node - finds actors within an area
/// </summary>
[System.Serializable]
public class TargetAreaNode : SkillGraphNode
{
    private const string CENTER_INPUT = "center_in";
    private const string RADIUS_INPUT = "radius_in";
    private const string ACTORS_OUTPUT = "actors_out";
    
    [SerializeField] private AreaShape shape = AreaShape.Sphere;
    [SerializeField] private float radius = 5f;
    [SerializeField] private Vector3 size = Vector3.one;
    [SerializeField] private Vector3 defaultCenter = Vector3.zero;
    [SerializeField] private bool useInputCenter = false;
    [SerializeField] private bool useInputRadius = false;
    [SerializeField] private bool targetEnemies = true;
    [SerializeField] private bool targetAllies = false;
    [SerializeField] private bool includeSelf = false;
    
    public TargetAreaNode() : base()
    {
        SetTitle("Target Area");
        SetDescription("Finds actors within an area");
        shape = AreaShape.Sphere;
        radius = 5f;
        size = Vector3.one;
        defaultCenter = Vector3.zero;
        useInputCenter = false;
        useInputRadius = false;
        targetEnemies = true;
        targetAllies = false;
        includeSelf = false;
    }
    
    public TargetAreaNode(Vector2 pos) : base(pos)
    {
        SetTitle("Target Area");
        SetDescription("Finds actors within an area");
        shape = AreaShape.Sphere;
        radius = 5f;
        size = Vector3.one;
        defaultCenter = Vector3.zero;
        useInputCenter = false;
        useInputRadius = false;
        targetEnemies = true;
        targetAllies = false;
        includeSelf = false;
    }
    
    public AreaShape Shape
    {
        get => shape;
        set => shape = value;
    }
    
    public float Radius
    {
        get => radius;
        set
        {
            if (value < 0) value = 0;
            radius = value;
        }
    }
    
    public Vector3 Size
    {
        get => size;
        set => size = value;
    }
    
    public Vector3 DefaultCenter
    {
        get => defaultCenter;
        set => defaultCenter = value;
    }
    
    public bool UseInputCenter
    {
        get => useInputCenter;
        set
        {
            if (useInputCenter != value)
            {
                useInputCenter = value;
                ValidateAndCleanupConnections();
            }
        }
    }
    
    public bool UseInputRadius
    {
        get => useInputRadius;
        set
        {
            if (useInputRadius != value)
            {
                useInputRadius = value;
                ValidateAndCleanupConnections();
            }
        }
    }
    
    public bool TargetEnemies
    {
        get => targetEnemies;
        set => targetEnemies = value;
    }
    
    public bool TargetAllies
    {
        get => targetAllies;
        set => targetAllies = value;
    }
    
    public bool IncludeSelf
    {
        get => includeSelf;
        set => includeSelf = value;
    }
    
    public override List<NodePin> GetInputPins()
    {
        var pins = new List<NodePin>();
        
        if (useInputCenter)
        {
            pins.Add(new NodePin(CENTER_INPUT, "Center", PinType.Data, PinDirection.Input, NodePinHelper.PinDataTypes.Vector3));
        }
        
        if (useInputRadius)
        {
            pins.Add(new NodePin(RADIUS_INPUT, "Radius", PinType.Data, PinDirection.Input, NodePinHelper.PinDataTypes.Float));
        }
        
        return pins;
    }
    
    public override List<NodePin> GetOutputPins()
    {
        return new List<NodePin>
        {
            new NodePin(ACTORS_OUTPUT, "Actors", PinType.Data, PinDirection.Output, NodePinHelper.PinDataTypes.ActorList)
        };
    }
}

[System.Serializable]
public enum AreaShape
{
    Sphere,
    Box,
    Capsule
}

