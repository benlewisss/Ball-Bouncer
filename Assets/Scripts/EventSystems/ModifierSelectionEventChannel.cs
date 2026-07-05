using System;
using UnityEngine;

/// <summary>
/// Event channel for when a modifier is selected
/// </summary>
[CreateAssetMenu(fileName = "ModifierSelectionEventChannel", menuName = "Events/ModifierSelectionEventChannel")]
public class ModifierSelectionEventChannel : ScriptableObject
{
    /// <summary>
    /// Event triggered when a specific modifier is selected
    /// </summary>
    public event Action<GenericModifierData> OnModifierSelected;

    /// <summary>
    /// Broadcasts the selection event
    /// </summary>
    /// <param name="modifier">The modifier data that was selected, or null if a modifier was deselected</param>
    public void RaiseEvent(GenericModifierData modifier)
    {
        OnModifierSelected?.Invoke(modifier);
    }
}