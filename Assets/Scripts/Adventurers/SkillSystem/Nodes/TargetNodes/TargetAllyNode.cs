using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Target Ally node - finds and targets ally actors
/// </summary>
[System.Serializable]
public class TargetAllyNode : SkillGraphNode
{
    private const string ALLIES_OUTPUT = "allies_out";
    private const string FIRST_ALLY_OUTPUT = "first_ally_out";
    
    [SerializeField] private int maxTargets = 1;
    [SerializeField] private float searchRadius = 10f;
    [SerializeField] private bool useCustomRadius = false;
    [SerializeField] private bool includeSelf = true;
    
    public TargetAllyNode() : base()
    {
        SetTitle("Target Ally");
        SetDescription("Finds and targets ally actors");
        maxTargets = 1;
        searchRadius = 10f;
        useCustomRadius = false;
        includeSelf = true;
    }
    
    public TargetAllyNode(Vector2 pos) : base(pos)
    {
        SetTitle("Target Ally");
        SetDescription("Finds and targets ally actors");
        maxTargets = 1;
        searchRadius = 10f;
        useCustomRadius = false;
        includeSelf = true;
    }
    
    public int MaxTargets
    {
        get => maxTargets;
        set
        {
            if (value < 1) value = 1;
            maxTargets = value;
        }
    }
    
    public float SearchRadius
    {
        get => searchRadius;
        set
        {
            if (value < 0) value = 0;
            searchRadius = value;
        }
    }
    
    public bool UseCustomRadius
    {
        get => useCustomRadius;
        set => useCustomRadius = value;
    }
    
    public bool IncludeSelf
    {
        get => includeSelf;
        set => includeSelf = value;
    }
    
    public override List<NodePin> GetInputPins()
    {
        var pins = new List<NodePin>();
        
        if (useCustomRadius)
        {
            pins.Add(new NodePin("radius_in", "Radius", PinType.Data, PinDirection.Input, NodePinHelper.PinDataTypes.Float));
        }
        
        return pins;
    }
    
    public override List<NodePin> GetOutputPins()
    {
        var pins = new List<NodePin>
        {
            new NodePin(ALLIES_OUTPUT, "Allies", PinType.Data, PinDirection.Output, NodePinHelper.PinDataTypes.ActorList)
        };
        
        if (maxTargets >= 1)
        {
            pins.Add(new NodePin(FIRST_ALLY_OUTPUT, "First Ally", PinType.Data, PinDirection.Output, NodePinHelper.PinDataTypes.Actor));
        }
        
        return pins;
    }
}

