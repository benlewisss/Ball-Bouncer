using UnityEngine;

public class InputManager : MonoBehaviour
{
    // Reference to the auto-generated wrapper class from the Unity Input System
    private GameInput _gameInput;

    private void Awake()
    {
        _gameInput = new GameInput();
    }

    private void OnEnable()
    {
        _gameInput.Gameplay.Enable();
    }

    private void OnDisable()
    {
        _gameInput.Gameplay.Disable();
    }

    public bool InteractPerformed()
    {
        return _gameInput.Gameplay.Interact.WasPerformedThisFrame();
    }

    public bool RemoveTargetPerformed()
    {
        return _gameInput.Gameplay.Remove.WasPerformedThisFrame();
    }

    public bool UndoPerformed()
    {
        return _gameInput.Gameplay.Undo.WasPerformedThisFrame();
    }

    public bool SimulatePerformed()
    {
        return _gameInput.Gameplay.Simulate.WasPerformedThisFrame();
    }

    public bool ResetPerformed()
    {
        return _gameInput.Gameplay.Reset.WasPerformedThisFrame();
    }

    public bool DebugPerformed()
    {
        return _gameInput.Gameplay.Debug.WasPerformedThisFrame();
    }

    public bool PausePerformed()
    {
        return _gameInput.Gameplay.Pause.WasPerformedThisFrame();
    }

    public Vector2 GetCursorPosition()
    {
        return _gameInput.Gameplay.CursorPosition.ReadValue<Vector2>();
    }
}