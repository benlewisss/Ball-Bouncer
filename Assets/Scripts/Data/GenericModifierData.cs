using UnityEngine;

/// <summary>
/// Base class for all modifiers that can be applied to interactable objects.
/// ScriptableObjects store data that doesn't have to be attached to game objects.
/// </summary>
public abstract class GenericModifierData : ScriptableObject
{
    public string modifierName;

    [Tooltip("Short description of what this modifier does (35 char max ish)")]
    [TextArea(2, 3)]
    public string effectDescription;
    public Color modifierColor = Color.white;
    public Material modifierMaterial;

    /// <summary>
    /// Applies the specific modifier effect to the given target.
    /// </summary>
    /// <param name="target">The target object receiving the modifier.</param>
    public abstract void Apply(InteractableObject target);

    /// <summary>
    /// Reverts the specific modifier effect from the given target.
    /// </summary>
    /// <param name="target">The target object losing the modifier.</param>
    public abstract void Revert(InteractableObject target);
}