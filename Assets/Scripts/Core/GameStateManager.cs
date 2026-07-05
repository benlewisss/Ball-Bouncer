using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public enum GameState
{
    Setup,
    Simulate,
    Win,
    Lose
}

/// <summary>
/// The main game loop manager. A state machine that handles switching between the setup/simulate stages
/// </summary>
/// 
[DefaultExecutionOrder(10)]
public class GameStateManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private InputManager _inputManager;
    [SerializeField] private BallActor _ball;
    [SerializeField] private LevelEndEventChannel _levelEndChannel;

    [Header("Effects")]
    [SerializeField] private CameraShake _cameraShake;

    [Header("Audio")]
    [SerializeField] private AudioCueData _loseSound;
    [SerializeField] private AudioCueEventChannel _audioChannel;

    /// <summary>
    /// The current state of the game loop
    /// </summary>
    public GameState CurrentState { get; private set; }

    /// <summary>
    /// Event fired whenever the game state transitions to a new state. Contains the new state it transitioned to.
    /// </summary>
    public event Action<GameState> OnGameStateChanged;

    private List<IResettable> _resetList = new List<IResettable>();

    private void Start()
    {
        // Initialise a list of all Resettable objects in the scene
        _resetList = FindObjectsByType<MonoBehaviour>().OfType<IResettable>().ToList();

        foreach (IResettable resettable in _resetList)
        {
            resettable.RecordInitialState();
        }

        ChangeState(GameState.Setup);
    }

    private void OnEnable()
    {
        // Subscribe to level end events
        if (_levelEndChannel != null)
        {
            _levelEndChannel.OnLevelEnded += HandleLevelEnd;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from level end events
        if (_levelEndChannel != null)
        {
            _levelEndChannel.OnLevelEnded -= HandleLevelEnd;
        }
    }

    private void Update()
    {
        if (_inputManager.SimulatePerformed() && CurrentState == GameState.Setup)
        {
            ChangeState(GameState.Simulate);
        }
        else if (_inputManager.ResetPerformed())
        {
            PerformHardReset();
            ChangeState(GameState.Setup);
        }
    }

    /// <summary>
    /// Transitions the game to a new state
    /// </summary>
    public void ChangeState(GameState newState)
    {
        CurrentState = newState;
        OnGameStateChanged?.Invoke(CurrentState);

        switch (CurrentState)
        {
            case GameState.Setup:
                ExecuteSetupState();
                break;
            case GameState.Simulate:
                ExecuteSimulateState();
                break;
            case GameState.Win:
                ExecuteWinState();
                break;
            case GameState.Lose:
                ExecuteLoseState();
                break;
        }
    }

    private void HandleLevelEnd(bool isWin)
    {
        if (CurrentState != GameState.Simulate)
        {
            return;
        }

        if (isWin)
        {
            ChangeState(GameState.Win);
        }
        else
        {
            ChangeState(GameState.Lose);
        }
    }

    private void PerformHardReset()
    {
        StopAllCoroutines();
        foreach (IResettable resettable in _resetList)
        {
            resettable.HardReset();
        }

        Debug.Log($"<color=red>{gameObject.name}: Hard Reset - reset everything to initial state</color>");
    }

    private void ExecuteSetupState()
    {
        // SimulationMode2D.Script means physics will only calculate if manually specified in a script,
        // otherwise everything will be paused
        Physics2D.simulationMode = SimulationMode2D.Script;

        // Explicitly disable ball simulation to stop the BallActor stationary timer from running 
        _ball.SetSimulated(false);

        foreach (IResettable resettable in _resetList)
        {
            resettable.SoftReset();
        }
    }

    private void ExecuteSimulateState()
    {
        Physics2D.simulationMode = SimulationMode2D.FixedUpdate;

        _ball.SetSimulated(true);
    }

    private void ExecuteWinState()
    {
        Debug.Log($"<color=red>{gameObject.name}: Level Complete! Player Wins!</color>");
    }

    private void ExecuteLoseState()
    {
        _ball.SetSimulated(false);
        Debug.Log($"<color=red>{gameObject.name}: Player Loses!</color>");

        _audioChannel?.RaiseEvent(_loseSound, Vector3.zero);

        if (_cameraShake != null)
        {
            StartCoroutine(_cameraShake.Shake(0.4f));
        }

        ChangeState(GameState.Setup);
    }
}