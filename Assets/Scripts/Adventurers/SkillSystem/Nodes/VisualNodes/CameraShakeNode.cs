using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Camera Shake node - applies camera shake effect
/// </summary>
[System.Serializable]
public class CameraShakeNode : SkillGraphNode
{
    private const string EXEC_INPUT = "exec_in";
    private const string EXEC_OUTPUT = "exec_out";
    private const string INTENSITY_INPUT = "intensity_in";
    private const string DURATION_INPUT = "duration_in";
    
    [SerializeField] private ShakeType shakeType = ShakeType.Position;
    [SerializeField] private float intensity = 1f;
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private bool useInputIntensity = false;
    [SerializeField] private bool useInputDuration = false;
    [SerializeField] private AnimationCurve falloffCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    
    public CameraShakeNode() : base()
    {
        SetTitle("Camera Shake");
        SetDescription("Applies camera shake effect");
        shakeType = ShakeType.Position;
        intensity = 1f;
        duration = 0.5f;
        useInputIntensity = false;
        useInputDuration = false;
        falloffCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    }
    
    public CameraShakeNode(Vector2 pos) : base(pos)
    {
        SetTitle("Camera Shake");
        SetDescription("Applies camera shake effect");
        shakeType = ShakeType.Position;
        intensity = 1f;
        duration = 0.5f;
        useInputIntensity = false;
        useInputDuration = false;
        falloffCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    }
    
    public ShakeType ShakeType
    {
        get => shakeType;
        set => shakeType = value;
    }
    
    public float Intensity
    {
        get => intensity;
        set
        {
            if (value < 0) value = 0;
            intensity = value;
        }
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
    
    public bool UseInputIntensity
    {
        get => useInputIntensity;
        set
        {
            if (useInputIntensity != value)
            {
                useInputIntensity = value;
                ValidateAndCleanupConnections();
            }
        }
    }
    
    public bool UseInputDuration
    {
        get => useInputDuration;
        set
        {
            if (useInputDuration != value)
            {
                useInputDuration = value;
                ValidateAndCleanupConnections();
            }
        }
    }
    
    public AnimationCurve FalloffCurve
    {
        get => falloffCurve;
        set => falloffCurve = value;
    }
    
    public override List<NodePin> GetInputPins()
    {
        var pins = new List<NodePin>
        {
            new NodePin(EXEC_INPUT, "Execute", PinType.Execution, PinDirection.Input)
        };
        
        if (useInputIntensity)
        {
            pins.Add(new NodePin(INTENSITY_INPUT, "Intensity", PinType.Data, PinDirection.Input, NodePinHelper.PinDataTypes.Float));
        }
        
        if (useInputDuration)
        {
            pins.Add(new NodePin(DURATION_INPUT, "Duration", PinType.Data, PinDirection.Input, NodePinHelper.PinDataTypes.Float));
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
public enum ShakeType
{
    Position,
    Rotation,
    Both
}

