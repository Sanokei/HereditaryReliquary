using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Target Enemy node - finds and targets enemy actors
/// </summary>
[System.Serializable]
public class TargetEnemyNode : SkillGraphNode
{
    private const string ENEMIES_OUTPUT = "enemies_out";
    private const string FIRST_ENEMY_OUTPUT = "first_enemy_out";
    
    [SerializeField] private int maxTargets = 1;
    [SerializeField] private float searchRadius = 10f;
    [SerializeField] private bool useCustomRadius = false;
    
    public TargetEnemyNode() : base()
    {
        SetTitle("Target Enemy");
        SetDescription("Finds and targets enemy actors");
        maxTargets = 1;
        searchRadius = 10f;
        useCustomRadius = false;
    }
    
    public TargetEnemyNode(Vector2 pos) : base(pos)
    {
        SetTitle("Target Enemy");
        SetDescription("Finds and targets enemy actors");
        maxTargets = 1;
        searchRadius = 10f;
        useCustomRadius = false;
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
            new NodePin(ENEMIES_OUTPUT, "Enemies", PinType.Data, PinDirection.Output, NodePinHelper.PinDataTypes.ActorList)
        };
        
        if (maxTargets >= 1)
        {
            pins.Add(new NodePin(FIRST_ENEMY_OUTPUT, "First Enemy", PinType.Data, PinDirection.Output, NodePinHelper.PinDataTypes.Actor));
        }
        
        return pins;
    }
}

