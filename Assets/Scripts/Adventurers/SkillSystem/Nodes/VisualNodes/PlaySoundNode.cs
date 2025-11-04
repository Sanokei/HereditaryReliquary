using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Play Sound node - plays an audio clip
/// </summary>
[System.Serializable]
public class PlaySoundNode : SkillGraphNode
{
    private const string EXEC_INPUT = "exec_in";
    private const string EXEC_OUTPUT = "exec_out";
    private const string POSITION_INPUT = "position_in";
    
    [SerializeField] private AudioClip audioClip;
    [SerializeField] private float volume = 1f;
    [SerializeField] private float pitch = 1f;
    [SerializeField] private bool loop = false;
    [SerializeField] private bool useInputPosition = false;
    [SerializeField] private Vector3 defaultPosition = Vector3.zero;
    [SerializeField] private bool playAtActor = true;
    
    public PlaySoundNode() : base()
    {
        SetTitle("Play Sound");
        SetDescription("Plays an audio clip");
        audioClip = null;
        volume = 1f;
        pitch = 1f;
        loop = false;
        useInputPosition = false;
        defaultPosition = Vector3.zero;
        playAtActor = true;
    }
    
    public PlaySoundNode(Vector2 pos) : base(pos)
    {
        SetTitle("Play Sound");
        SetDescription("Plays an audio clip");
        audioClip = null;
        volume = 1f;
        pitch = 1f;
        loop = false;
        useInputPosition = false;
        defaultPosition = Vector3.zero;
        playAtActor = true;
    }
    
    public AudioClip AudioClip
    {
        get => audioClip;
        set => audioClip = value;
    }
    
    public float Volume
    {
        get => volume;
        set
        {
            if (value < 0) value = 0;
            if (value > 1) value = 1;
            volume = value;
        }
    }
    
    public float Pitch
    {
        get => pitch;
        set
        {
            if (value < 0.1f) value = 0.1f;
            if (value > 3f) value = 3f;
            pitch = value;
        }
    }
    
    public bool Loop
    {
        get => loop;
        set => loop = value;
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
    
    public bool PlayAtActor
    {
        get => playAtActor;
        set => playAtActor = value;
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

