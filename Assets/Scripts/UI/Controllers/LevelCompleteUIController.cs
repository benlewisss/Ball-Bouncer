using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class LevelCompleteUI : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private GameStateManager _stateManager;
    [SerializeField] private InputManager _inputManager;

    [Header("UI Text Box References")]
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private TextMeshProUGUI _highScoreText;
    [SerializeField] private TextMeshProUGUI _gradeText;

    [Header("UI Button References")]
    [SerializeField] private Button _nextLevelButton;
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _mainMenuButton;

    [Header("Main Menu Scene")]
    [SerializeField] private int _mainMenuBuildIndex = 0;

    [Header("Audio")]
    [SerializeField] private AudioCueEventChannel _audioChannel;
    [SerializeField] private AudioCueData _winBadScoreSound;
    [SerializeField] private AudioCueData _winMediumScoreSound;
    [SerializeField] private AudioCueData _winGoodScoreSound;
    
    private CanvasGroup _canvasGroup;
    private bool _isMenuVisible = false;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();

        SetMenuVisible(false);

        // UI button clicks bindings
        if (_nextLevelButton != null)
        {
            _nextLevelButton.onClick.AddListener(LoadNextLevel);
        }

        if (_restartButton != null)
        {
            _restartButton.onClick.AddListener(RestartLevel);
        }

        if (_mainMenuButton != null)
        {
            _mainMenuButton.onClick.AddListener(LoadMainMenu);
        }
    }

    private void OnEnable()
    {
        // Subscribe to game state change events
        if (_stateManager != null)
        {
            _stateManager.OnGameStateChanged += HandleGameStateChanged;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe to game state change events
        if (_stateManager != null)
        {
            _stateManager.OnGameStateChanged -= HandleGameStateChanged;
        }
    }

    private void Update()
    {
        if (!_isMenuVisible)
        {
            return;
        }

        if (_inputManager.SimulatePerformed())
        {
            LoadNextLevel();
        }
        else if (_inputManager.ResetPerformed())
        {
            RestartLevel();
        }
    }

    /// <summary>
    /// Loads the next level based on the build settings level indexing order
    /// </summary>
    public void LoadNextLevel()
    {
        // The build index is the order of scenes defined in the Unity Build Settings
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;

        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            Debug.Log($"{gameObject.name}: Loading next level!");
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            Debug.Log($"{gameObject.name}: Game Complete! No more levels.");
        }
    }

    /// <summary>
    /// Reloads the current active scene (similar to a hard reset but also resets GUI and managers)
    /// </summary>
    public void RestartLevel()
    {
        _stateManager.ChangeState(GameState.Setup);
        SetMenuVisible(false);
    }

    private void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(_mainMenuBuildIndex);
    }

    private void HandleGameStateChanged(GameState state)
    {
        if (state == GameState.Win)
        {
            DisplayResults();
        }
        else {
            SetMenuVisible(false);
        }
    }

    private void DisplayResults()
    {
        int totalModifiersUsed = 0;

        InteractableObject[] allTargets = FindObjectsByType<InteractableObject>();

        foreach (InteractableObject target in allTargets)
        {
            totalModifiersUsed += target.GetAppliedModifierCount();
        }

        if (LevelScoreManager.Instance != null)
        {
            var results = LevelScoreManager.Instance.StoreResults(totalModifiersUsed);

            if (_scoreText != null)
            {
                _scoreText.text = $"{results.finalScore}";
            }

            if (_highScoreText != null)
            {
                _highScoreText.text = $"{results.highScore}";
            }

            if (_gradeText != null)
            {
                _gradeText.text = results.finalGrade;
            }

            PlayWinSound(results.finalGrade);
        }

        SetMenuVisible(true);
    }

    private void SetMenuVisible(bool visible)
    {
        if (_canvasGroup == null) return;
        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }

    private void PlayWinSound(string grade)
    {
        AudioCueData sound = _winBadScoreSound;

        if (grade == "C" || grade == "B")
        {
            sound = _winMediumScoreSound;
        }
        else if (grade == "A" || grade == "A+")
        {
            sound = _winGoodScoreSound;
        }

        _audioChannel?.RaiseEvent(sound, Vector3.zero);
    }

    /// <summary>
    /// Debug method to hide level end UI forcefully
    /// </summary>
    public void ForceHide()
    {
        SetMenuVisible(false);
    }
}