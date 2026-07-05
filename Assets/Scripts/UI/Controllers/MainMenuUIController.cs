using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUIController : MonoBehaviour
{
    [System.Serializable]
    public class LevelButton
    {
        public Button button;
        public int sceneBuildIndex;
    }

    [Header("Panels")]
    [SerializeField] private GameObject _mainPanel;
    [SerializeField] private GameObject _levelSelectPanel;

    [Header("UI Main Menu Button References")]
    [SerializeField] private Button _playButton;
    [SerializeField] private Button _levelSelectButton;
    [SerializeField] private Button _quitButton;

    [Header("UI Level Select References")]
    [SerializeField] private LevelButton[] _levels;
    [SerializeField] private Button _backButton;

    [Header("First Level Scene")]
    [SerializeField] private int _firstLevelBuildIndex = 1;

    private void Awake()
    {
        // UI button clicks bindings

        if (_playButton != null)
        {
            _playButton.onClick.AddListener(() => SceneManager.LoadScene(_firstLevelBuildIndex));
        }

        if (_levelSelectButton != null)
        {
            _levelSelectButton.onClick.AddListener(() => ShowPanel(true));
        }

        if (_quitButton != null)
        {
            _quitButton.onClick.AddListener(QuitGame);
        }  

        if (_backButton != null)
        {
            _backButton.onClick.AddListener(() => ShowPanel(false));
        }  

        SetupLevelButtons();
    }

    private void Start()
    {
        ShowPanel(false);
        Time.timeScale = 1.0f;
    }

    private void ShowPanel(bool levelSelect)
    {
        _mainPanel.SetActive(!levelSelect);
        _levelSelectPanel.SetActive(levelSelect);
    }

    private void SetupLevelButtons()
    {
        if (_levels == null) return;

        foreach (LevelButton level in _levels)
        {
            if (level.button == null) continue;

            int buildIndex = level.sceneBuildIndex;
            level.button.onClick.AddListener(() => SceneManager.LoadScene(buildIndex));

            // Level select box has a text field called "GradeText" which is what holds the grade
            string savedGrade = PlayerPrefs.GetString($"Level_{buildIndex}_Grade", "");

            foreach (TextMeshProUGUI textBox in level.button.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (textBox.gameObject.name != "GradeText") continue;

                if (!string.IsNullOrEmpty(savedGrade))
                {
                    textBox.text = savedGrade;
                    textBox.gameObject.SetActive(true);
                }
                else
                {
                    textBox.gameObject.SetActive(false);
                }
                break;
            }
        }
    }

    private void QuitGame()
    {
        Application.Quit();
    }
}