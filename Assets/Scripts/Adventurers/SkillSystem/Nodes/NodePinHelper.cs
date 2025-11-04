using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Helper class for working with node pins and type-safe connections
/// </summary>
public static class NodePinHelper
{
    /// <summary>
    /// Common data types used in skill graphs
    /// </summary>
    public static class PinDataTypes
    {
        public static readonly Type Int = typeof(int);
        public static readonly Type Float = typeof(float);
        public static readonly Type Bool = typeof(bool);
        public static readonly Type String = typeof(string);
        public static readonly Type Vector3 = typeof(UnityEngine.Vector3);
        public static readonly Type Actor = typeof(IActor);
        public static readonly Type ActorList = typeof(List<IActor>);
        public static readonly Type GameObject = typeof(UnityEngine.GameObject);
        public static readonly Type Transform = typeof(UnityEngine.Transform);
    }
    
    /// <summary>
    /// Checks if two pins can be connected (type compatibility)
    /// </summary>
    public static bool CanConnectPins(NodePin outputPin, NodePin inputPin)
    {
        if (outputPin == null || inputPin == null)
            return false;
        
        // Execution pins can only connect to execution pins
        if (outputPin.Type == PinType.Execution && inputPin.Type == PinType.Execution)
        {
            return outputPin.Direction == PinDirection.Output && inputPin.Direction == PinDirection.Input;
        }
        
        // Data pins must match types
        if (outputPin.Type == PinType.Data && inputPin.Type == PinType.Data)
        {
            if (outputPin.Direction != PinDirection.Output || inputPin.Direction != PinDirection.Input)
                return false;
            
            // Check type compatibility
            return AreTypesCompatible(outputPin.DataType, inputPin.DataType);
        }
        
        return false;
    }
    
    /// <summary>
    /// Checks if two types are compatible for pin connections
    /// </summary>
    public static bool AreTypesCompatible(Type outputType, Type inputType)
    {
        if (outputType == null || inputType == null)
            return false;
        
        // Exact match
        if (outputType == inputType)
            return true;
        
        // Nullable types
        if (IsNullable(outputType) && GetNullableInnerType(outputType) == inputType)
            return true;
        if (IsNullable(inputType) && outputType == GetNullableInnerType(inputType))
            return true;
        
        // Inheritance
        if (inputType.IsAssignableFrom(outputType))
            return true;
        
        // Numeric conversions (int to float, etc.)
        if (IsNumericType(outputType) && IsNumericType(inputType))
        {
            // Allow int -> float
            if (outputType == PinDataTypes.Int && inputType == PinDataTypes.Float)
                return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Gets a pin by ID from a list of pins
    /// </summary>
    public static NodePin GetPinById(List<NodePin> pins, string pinId)
    {
        return pins?.FirstOrDefault(p => p.Id == pinId);
    }
    
    /// <summary>
    /// Gets a pin by name from a list of pins
    /// </summary>
    public static NodePin GetPinByName(List<NodePin> pins, string pinName)
    {
        return pins?.FirstOrDefault(p => p.Name == pinName);
    }
    
    /// <summary>
    /// Checks if a type is numeric
    /// </summary>
    private static bool IsNumericType(Type type)
    {
        return type == typeof(int) || type == typeof(float) || type == typeof(double) || 
               type == typeof(long) || type == typeof(short) || type == typeof(byte) ||
               type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort);
    }
    
    /// <summary>
    /// Checks if a type is nullable
    /// </summary>
    private static bool IsNullable(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
    }
    
    /// <summary>
    /// Gets the inner type of a nullable type
    /// </summary>
    private static Type GetNullableInnerType(Type nullableType)
    {
        return nullableType.GetGenericArguments()[0];
    }
    
    /// <summary>
    /// Gets a human-readable type name for display
    /// </summary>
    public static string GetTypeDisplayName(Type type)
    {
        if (type == null)
            return "Void";
        
        if (type == PinDataTypes.Int)
            return "Int";
        if (type == PinDataTypes.Float)
            return "Float";
        if (type == PinDataTypes.Bool)
            return "Bool";
        if (type == PinDataTypes.String)
            return "String";
        if (type == PinDataTypes.Vector3)
            return "Vector3";
        if (type == PinDataTypes.Actor)
            return "Actor";
        if (type == PinDataTypes.ActorList)
            return "Actor List";
        if (type == PinDataTypes.GameObject)
            return "GameObject";
        if (type == PinDataTypes.Transform)
            return "Transform";
        
        // Remove namespace if it's a common Unity type
        string name = type.Name;
        if (name.StartsWith("List`1"))
        {
            var genericArgs = type.GetGenericArguments();
            if (genericArgs.Length > 0)
            {
                return $"List<{GetTypeDisplayName(genericArgs[0])}>";
            }
        }
        
        return name;
    }
}

