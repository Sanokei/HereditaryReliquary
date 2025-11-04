using UnityEngine;

/// <summary>
/// Interface for actors that can perform skills
/// </summary>
public interface IActor
{
    string Name { get; }
    Transform Transform { get; }
    // Add any other actor properties needed
}

