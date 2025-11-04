using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawn node - spawns objects/projectiles at a position
/// </summary>
[System.Serializable]
public class SpawnNode : SkillGraphNode
{
    private const string EXEC_INPUT = "exec_in";
    private const string EXEC_OUTPUT = "exec_out";
    private const string POSITION_INPUT = "position_in";
    private const string ROTATION_INPUT = "rotation_in";
    private const string SPAWNED_OUTPUT = "spawned_out";
    
    [SerializeField] private GameObject prefab;
    [SerializeField] private bool useInputPosition = true;
    [SerializeField] private bool useInputRotation = false;
    [SerializeField] private Vector3 defaultPosition = Vector3.zero;
    [SerializeField] private Quaternion defaultRotation = Quaternion.identity;
    
    public SpawnNode() : base()
    {
        SetTitle("Spawn");
        SetDescription("Spawns objects/projectiles");
        prefab = null;
        useInputPosition = true;
        useInputRotation = false;
        defaultPosition = Vector3.zero;
        defaultRotation = Quaternion.identity;
    }
    
    public SpawnNode(Vector2 pos) : base(pos)
    {
        SetTitle("Spawn");
        SetDescription("Spawns objects/projectiles");
        prefab = null;
        useInputPosition = true;
        useInputRotation = false;
        defaultPosition = Vector3.zero;
        defaultRotation = Quaternion.identity;
    }
    
    public GameObject Prefab
    {
        get => prefab;
        set => prefab = value;
    }
    
    public bool UseInputPosition
    {
        get => useInputPosition;
        set => useInputPosition = value;
    }
    
    public bool UseInputRotation
    {
        get => useInputRotation;
        set => useInputRotation = value;
    }
    
    public Vector3 DefaultPosition
    {
        get => defaultPosition;
        set => defaultPosition = value;
    }
    
    public Quaternion DefaultRotation
    {
        get => defaultRotation;
        set => defaultRotation = value;
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
        
        if (useInputRotation)
        {
            // Note: Quaternion not in PinDataTypes, but we can use Vector3 for euler angles
            // Or add Quaternion support later
        }
        
        return pins;
    }
    
    public override List<NodePin> GetOutputPins()
    {
        return new List<NodePin>
        {
            new NodePin(EXEC_OUTPUT, "Complete", PinType.Execution, PinDirection.Output),
            new NodePin(SPAWNED_OUTPUT, "Spawned", PinType.Data, PinDirection.Output, NodePinHelper.PinDataTypes.GameObject)
        };
    }
}

