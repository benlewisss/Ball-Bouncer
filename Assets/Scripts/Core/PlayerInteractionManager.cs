using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerInteractionManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private InputManager _inputManager;
    [SerializeField] private GameStateManager _stateManager;
    [SerializeField] private ModifierSelectionEventChannel _selectionEventChannel;

    [Header("Audio")]
    [SerializeField] private AudioCueData _modifierApplySound;
    [SerializeField] private AudioCueData _modifierRemoveSound;
    [SerializeField] private AudioCueEventChannel _audioChannel;

    private GenericModifierData _currentSelectedModifier;
    private Camera _mainCamera;

    private void Awake()
    {
        _mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        // Subscribe to modifier selection events
        if (_selectionEventChannel != null)
        {
            _selectionEventChannel.OnModifierSelected += HandleModifierSelected;
        }

        // Subscribe to game state events
        if (_stateManager != null)
        {
            _stateManager.OnGameStateChanged += HandleGameStateChanged;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe to modifier selection events
        if (_selectionEventChannel != null)
        {
            _selectionEventChannel.OnModifierSelected -= HandleModifierSelected;
        }

        // Unubscribe to game state events
        if (_stateManager != null)
        {
            _stateManager.OnGameStateChanged -= HandleGameStateChanged;
        }
    }

    private void Update()
    {
        // Deselect the active modifier if player resets
        if (_inputManager.ResetPerformed() && _currentSelectedModifier != null)
        {
            _currentSelectedModifier = null;

            if (_selectionEventChannel != null)
            {
                _selectionEventChannel.RaiseEvent(null);
            }
        }

        // Only allow world interaction during the setup game state
        if (_stateManager.CurrentState != GameState.Setup)
        {
            return;
        }

        // If cursor is currently hovering over a UI element, then we ignore any player-world
        // interaction. This stops the player interacting with stuff underneath the UI by accident
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (_inputManager.InteractPerformed())
        {
            TryApplyModifier();
        }

        if (_inputManager.RemoveTargetPerformed())
        {
            TryRemoveTargetModifiers();
        }
    }

    private void TryApplyModifier()
    {
        if (_currentSelectedModifier == null)
        {
            return;
        }

        InteractableObject target = GetTargetUnderCursor();

        
        if (target == null) return;

        if (!target.CanAcceptModifier(_currentSelectedModifier)) return;

        // First remove the modifier that's current applied, if any
        if (target.GetAppliedModifierCount() > 0)
        {
            target.RevertAllModifiers();
        }

        _audioChannel?.RaiseEvent(_modifierApplySound, transform.position);
        target.ApplyModifier(_currentSelectedModifier);
    }

    private void TryRemoveTargetModifiers()
    {
        InteractableObject target = GetTargetUnderCursor();

        if (target != null && target.GetAppliedModifierCount() > 0)
        {
            _audioChannel?.RaiseEvent(_modifierRemoveSound, transform.position);
            target.RevertAllModifiers();
        }
    }

    private InteractableObject GetTargetUnderCursor()
    {
        Vector2 screenPosition = _inputManager.GetCursorPosition();
        Vector2 worldPosition = _mainCamera.ScreenToWorldPoint(screenPosition);

        // We have to raycast all so that the casts go through invisible volumes (like level bounds)
        RaycastHit2D[] hits = Physics2D.RaycastAll(worldPosition, Vector2.zero);

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider != null)
            {
                InteractableObject target = hit.collider.GetComponentInParent<InteractableObject>();
                if (target != null)
                {
                    return target;
                }
            }
        }

        return null;
    }

    private void HandleModifierSelected(GenericModifierData modifier)
    {
        _currentSelectedModifier = modifier;
    }

    private void HandleGameStateChanged(GameState newState)
    {
        // Deselect modifiers if the game transitions from setup state
        if (newState == GameState.Simulate && _currentSelectedModifier != null)
        {
            _currentSelectedModifier = null;

            if (_selectionEventChannel != null)
            {
                _selectionEventChannel.RaiseEvent(null);
            }
        }
    }
}