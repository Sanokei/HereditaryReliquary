using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Play Animation node - plays an animation on an actor
/// </summary>
[System.Serializable]
public class PlayAnimationNode : SkillGraphNode
{
    private const string EXEC_INPUT = "exec_in";
    private const string EXEC_OUTPUT = "exec_out";
    private const string ACTOR_INPUT = "actor_in";
    
    [SerializeField] private string animationName = "Attack";
    [SerializeField] private bool useInputAnimation = false;
    [SerializeField] private float animationSpeed = 1f;
    [SerializeField] private bool waitForCompletion = true;
    
    public PlayAnimationNode() : base()
    {
        SetTitle("Play Animation");
        SetDescription("Plays an animation on an actor");
        animationName = "Attack";
        useInputAnimation = false;
        animationSpeed = 1f;
        waitForCompletion = true;
    }
    
    public PlayAnimationNode(Vector2 pos) : base(pos)
    {
        SetTitle("Play Animation");
        SetDescription("Plays an animation on an actor");
        animationName = "Attack";
        useInputAnimation = false;
        animationSpeed = 1f;
        waitForCompletion = true;
    }
    
    public string AnimationName
    {
        get => animationName;
        set => animationName = value;
    }
    
    public bool UseInputAnimation
    {
        get => useInputAnimation;
        set
        {
            if (useInputAnimation != value)
            {
                useInputAnimation = value;
                ValidateAndCleanupConnections();
            }
        }
    }
    
    public float AnimationSpeed
    {
        get => animationSpeed;
        set
        {
            if (value < 0) value = 0;
            animationSpeed = value;
        }
    }
    
    public bool WaitForCompletion
    {
        get => waitForCompletion;
        set => waitForCompletion = value;
    }
    
    public override List<NodePin> GetInputPins()
    {
        var pins = new List<NodePin>
        {
            new NodePin(EXEC_INPUT, "Execute", PinType.Execution, PinDirection.Input),
            new NodePin(ACTOR_INPUT, "Actor", PinType.Data, PinDirection.Input, NodePinHelper.PinDataTypes.Actor)
        };
        
        if (useInputAnimation)
        {
            pins.Add(new NodePin("animation_in", "Animation Name", PinType.Data, PinDirection.Input, NodePinHelper.PinDataTypes.String));
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

