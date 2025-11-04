using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Constant node - provides constant values
/// </summary>
[System.Serializable]
public class ConstantNode : SkillGraphNode
{
    private const string VALUE_OUTPUT = "value_out";
    
    [SerializeField] private ConstantValueType valueType = ConstantValueType.Float;
    [SerializeField] private float floatValue = 0f;
    [SerializeField] private int intValue = 0;
    [SerializeField] private bool boolValue = false;
    [SerializeField] private string stringValue = "";
    [SerializeField] private Vector3 vector3Value = Vector3.zero;
    
    public ConstantNode() : base()
    {
        SetTitle("Constant");
        SetDescription("Provides a constant value");
        valueType = ConstantValueType.Float;
    }
    
    public ConstantNode(Vector2 pos) : base(pos)
    {
        SetTitle("Constant");
        SetDescription("Provides a constant value");
        valueType = ConstantValueType.Float;
    }
    
    public ConstantValueType ValueType
    {
        get => valueType;
        set => valueType = value;
    }
    
    public float FloatValue
    {
        get => floatValue;
        set => floatValue = value;
    }
    
    public int IntValue
    {
        get => intValue;
        set => intValue = value;
    }
    
    public bool BoolValue
    {
        get => boolValue;
        set => boolValue = value;
    }
    
    public string StringValue
    {
        get => stringValue;
        set => stringValue = value;
    }
    
    public Vector3 Vector3Value
    {
        get => vector3Value;
        set => vector3Value = value;
    }
    
    public override List<NodePin> GetInputPins()
    {
        return new List<NodePin>();
    }
    
    public override List<NodePin> GetOutputPins()
    {
        System.Type dataType = GetDataTypeForValueType(valueType);
        return new List<NodePin>
        {
            new NodePin(VALUE_OUTPUT, "Value", PinType.Data, PinDirection.Output, dataType)
        };
    }
    
    private System.Type GetDataTypeForValueType(ConstantValueType type)
    {
        switch (type)
        {
            case ConstantValueType.Int:
                return NodePinHelper.PinDataTypes.Int;
            case ConstantValueType.Float:
                return NodePinHelper.PinDataTypes.Float;
            case ConstantValueType.Bool:
                return NodePinHelper.PinDataTypes.Bool;
            case ConstantValueType.String:
                return NodePinHelper.PinDataTypes.String;
            case ConstantValueType.Vector3:
                return NodePinHelper.PinDataTypes.Vector3;
            default:
                return NodePinHelper.PinDataTypes.Float;
        }
    }
}

[System.Serializable]
public enum ConstantValueType
{
    Int,
    Float,
    Bool,
    String,
    Vector3
}

