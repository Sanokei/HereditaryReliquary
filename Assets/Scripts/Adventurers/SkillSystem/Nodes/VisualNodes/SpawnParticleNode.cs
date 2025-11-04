using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawn Particle node - spawns particle effects at a position
/// </summary>
[System.Serializable]
public class SpawnParticleNode : SkillGraphNode
{
    private const string EXEC_INPUT = "exec_in";
    private const string EXEC_OUTPUT = "exec_out";
    private const string POSITION_INPUT = "position_in";
    private const string SCALE_INPUT = "scale_in";
    
    [SerializeField] private GameObject particlePrefab;
    [SerializeField] private bool useInputPosition = true;
    [SerializeField] private Vector3 defaultPosition = Vector3.zero;
    [SerializeField] private float duration = 2f;
    [SerializeField] private bool autoDestroy = true;
    [SerializeField] private Vector3 scale = Vector3.one;
    [SerializeField] private bool useInputScale = false;
    [SerializeField] private Transform parentTransform = null;
    
    public SpawnParticleNode() : base()
    {
        SetTitle("Spawn Particle");
        SetDescription("Spawns particle effects");
        particlePrefab = null;
        useInputPosition = true;
        defaultPosition = Vector3.zero;
        duration = 2f;
        autoDestroy = true;
        scale = Vector3.one;
        useInputScale = false;
        parentTransform = null;
    }
    
    public SpawnParticleNode(Vector2 pos) : base(pos)
    {
        SetTitle("Spawn Particle");
        SetDescription("Spawns particle effects");
        particlePrefab = null;
        useInputPosition = true;
        defaultPosition = Vector3.zero;
        duration = 2f;
        autoDestroy = true;
        scale = Vector3.one;
        useInputScale = false;
        parentTransform = null;
    }
    
    public GameObject ParticlePrefab
    {
        get => particlePrefab;
        set => particlePrefab = value;
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
    
    public float Duration
    {
        get => duration;
        set
        {
            if (value < 0) value = 0;
            duration = value;
        }
    }
    
    public bool AutoDestroy
    {
        get => autoDestroy;
        set => autoDestroy = value;
    }
    
    public Vector3 Scale
    {
        get => scale;
        set => scale = value;
    }
    
    public bool UseInputScale
    {
        get => useInputScale;
        set
        {
            if (useInputScale != value)
            {
                useInputScale = value;
                ValidateAndCleanupConnections();
            }
        }
    }
    
    public Transform ParentTransform
    {
        get => parentTransform;
        set => parentTransform = value;
    }
    
    public override List<NodePin> GetInputPins()
    {
        var pins = new List<NodePin>
        {
            new NodePin(EXEC_INPUT, "Execute", PinType.Execution, PinDirection.Input)
        };
        
        if (useInputPosition)
        {
            pins.Add(new NodePin(POSITION_INPUT, "Position", PinType.Data, PinDirection.Input, NodePinHelper.PinDataTypes.Vector3));
        }
        
        if (useInputScale)
        {
            pins.Add(new NodePin(SCALE_INPUT, "Scale", PinType.Data, PinDirection.Input, NodePinHelper.PinDataTypes.Vector3));
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

