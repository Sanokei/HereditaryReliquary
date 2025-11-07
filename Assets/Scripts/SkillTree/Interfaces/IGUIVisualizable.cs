#if UNITY_EDITOR
using UnityEngine.UIElements;

/// <summary>
/// Optional interface for skills that can create GUI visualizations
/// </summary>
public interface IGUIVisualizable
{
    /// <summary>
    /// Creates a VisualElement for GUI representation
    /// </summary>
    VisualElement CreateGUI();
}
#endif

