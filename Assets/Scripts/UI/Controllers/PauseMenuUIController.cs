using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuUIController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private InputManager _inputManager;
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("UI Button References")]
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _mainMenuButton;
    [SerializeField] private Button _quitDesktopButton;

    [Header("Main Menu Scene")]
    [SerializeField] private int _mainMenuBuildIndex = 0;

    private bool _isPaused = false;
    private float _previousTimeScale = 1f;

    private void Awake()
    {
        if (_resumeButton != null)
        {
            _resumeButton.onClick.AddListener(Resume);
        }

        if (_mainMenuButton != null)
        {
            _mainMenuButton.onClick.AddListener(LoadMainMenu);
        }
            

        if (_quitDesktopButton != null)
        {
            _quitDesktopButton.onClick.AddListener(QuitGame);
        }

        SetMenuVisible(false);
    }

    private void Update()
    {
        if (_inputManager != null && _inputManager.PausePerformed())
        {
            if (_isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    private void Pause()
    {
        if (_isPaused) return;
        _isPaused = true;
        _previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        SetMenuVisible(true);
    }

    private void Resume()
    {
        if (!_isPaused) return;
        _isPaused = false;
        Time.timeScale = _previousTimeScale;
        SetMenuVisible(false);
    }

    private void SetMenuVisible(bool visible)
    {
        if (_canvasGroup == null) return;
        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }

    private void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(_mainMenuBuildIndex);
    }

    private void QuitGame()
    {
        Application.Quit();
    }
}