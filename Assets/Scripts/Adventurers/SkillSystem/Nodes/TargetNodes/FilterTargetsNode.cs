using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Filter Targets node - filters a list of actors based on conditions
/// </summary>
[System.Serializable]
public class FilterTargetsNode : SkillGraphNode
{
    private const string ACTORS_INPUT = "actors_in";
    private const string REFERENCE_POSITION_INPUT = "reference_position_in";
    private const string FILTERED_OUTPUT = "filtered_out";
    
    [SerializeField] private FilterType filterType = FilterType.Closest;
    [SerializeField] private int maxResults = 1;
    [SerializeField] private float minHealthPercent = 0f;
    [SerializeField] private bool useHealthFilter = false;
    [SerializeField] private bool useReferencePosition = false;
    [SerializeField] private Vector3 defaultReferencePosition = Vector3.zero;
    
    public FilterTargetsNode() : base()
    {
        SetTitle("Filter Targets");
        SetDescription("Filters a list of actors based on conditions");
        filterType = FilterType.Closest;
        maxResults = 1;
        minHealthPercent = 0f;
        useHealthFilter = false;
        useReferencePosition = false;
        defaultReferencePosition = Vector3.zero;
    }
    
    public FilterTargetsNode(Vector2 pos) : base(pos)
    {
        SetTitle("Filter Targets");
        SetDescription("Filters a list of actors based on conditions");
        filterType = FilterType.Closest;
        maxResults = 1;
        minHealthPercent = 0f;
        useHealthFilter = false;
        useReferencePosition = false;
        defaultReferencePosition = Vector3.zero;
    }
    
    public FilterType FilterType
    {
        get => filterType;
        set => filterType = value;
    }
    
    public int MaxResults
    {
        get => maxResults;
        set
        {
            if (value < 1) value = 1;
            maxResults = value;
        }
    }
    
    public float MinHealthPercent
    {
        get => minHealthPercent;
        set
        {
            if (value < 0) value = 0;
            if (value > 1) value = 1;
            minHealthPercent = value;
        }
    }
    
    public bool UseHealthFilter
    {
        get => useHealthFilter;
        set => useHealthFilter = value;
    }
    
    public bool UseReferencePosition
    {
        get => useReferencePosition;
        set
        {
            if (useReferencePosition != value)
            {
                useReferencePosition = value;
                ValidateAndCleanupConnections();
            }
        }
    }
    
    public Vector3 DefaultReferencePosition
    {
        get => defaultReferencePosition;
        set => defaultReferencePosition = value;
    }
    
    public override List<NodePin> GetInputPins()
    {
        var pins = new List<NodePin>
        {
            new NodePin(ACTORS_INPUT, "Actors", PinType.Data, PinDirection.Input, NodePinHelper.PinDataTypes.ActorList)
        };
        
        if (useReferencePosition || filterType == FilterType.Closest || filterType == FilterType.Farthest)
        {
            pins.Add(new NodePin(REFERENCE_POSITION_INPUT, "Reference Position", PinType.Data, PinDirection.Input, NodePinHelper.PinDataTypes.Vector3));
        }
        
        return pins;
    }
    
    public override List<NodePin> GetOutputPins()
    {
        return new List<NodePin>
        {
            new NodePin(FILTERED_OUTPUT, "Filtered", PinType.Data, PinDirection.Output, NodePinHelper.PinDataTypes.ActorList)
        };
    }
}

[System.Serializable]
public enum FilterType
{
    All,
    Closest,
    Farthest,
    LowestHealth,
    HighestHealth,
    Random
}

