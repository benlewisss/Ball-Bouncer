using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A trigger volume that stores modifiers and copies them onto interractable objects that pass through it
/// </summary>
[RequireComponent(typeof(Collider2D), typeof(InteractableObject))]
public class ModifierInfuser : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private GameStateManager _gameStateManager;

    [Header("Configuration")]
    [Tooltip("A list of objects that this infuser can affect - will affect any interactable object if empty.")]
    [SerializeField] private List<InteractableObject> _targetFilter = new List<InteractableObject>();
    [SerializeField] private bool _applyOnExit = false;

    [Header("Audio")]
    [SerializeField] private AudioCueData _modifierApplySound;
    [SerializeField] private AudioCueEventChannel _audioChannel;

    private InteractableObject _volumeInteractable;

    private void Awake()
    {
        _volumeInteractable = GetComponent<InteractableObject>();

        // Ensure the rigidbody is static so that in case I forgot to change this setting all the time, modifiers applied
        // to the volume don't cause it to fall
        Rigidbody2D rigidbody2d = GetComponent<Rigidbody2D>();
        if (rigidbody2d != null)
        {
            rigidbody2d.bodyType = RigidbodyType2D.Static;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_applyOnExit) return;

        ApplyModifier(collision);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!_applyOnExit) return;

        ApplyModifier(collision);
    }

    private void ApplyModifier(Collider2D collision)
    {
        // Don't apply modifiers in the setup stage
        if (_gameStateManager != null && _gameStateManager.CurrentState != GameState.Simulate) return;

        InteractableObject target = collision.GetComponent<InteractableObject>();

        // Ignore collisions with non-interactable objects or self
        if (target == null || target == _volumeInteractable)
        {
            return;
        }

        // explicit target filter
        if (_targetFilter.Count > 0 && !_targetFilter.Contains(target))
        {
            return;
        }

        // Copy all active modifiers from the volume onto the target object
        bool appliedModifier = false;
        foreach (GenericModifierData modifier in _volumeInteractable.GetActiveModifiers())
        {
            if (target.CanAcceptModifier(modifier, false))
            {
                // Modifiers applied by the volume don't consume player inventory counts so ApplyModifier isPaidFor = false
                target.ApplyModifier(modifier, false);
                appliedModifier = true;
            }
        }

        if (appliedModifier) _audioChannel?.RaiseEvent(_modifierApplySound, Vector3.zero);
    }
}