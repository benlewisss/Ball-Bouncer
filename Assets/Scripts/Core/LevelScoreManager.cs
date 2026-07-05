using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manager for level scoring
/// </summary>
// DefaultExecutionOrder makes sure this script Awake/Start method run before the other scripts (which default to 0).
// Need to do this for singleton managers as other scripts rely on them during initialisation.

[DefaultExecutionOrder(-10)]
public class LevelScoreManager : MonoBehaviour, IResettable
{
    [Header("Dependencies")]
    [SerializeField] private GameStateManager _stateManager;

    [Header("Score Configuration")]
    [Tooltip("Starting score before taking away the time penalty")]
    [SerializeField] private int _baseScore = 1000;
    [Tooltip("Points lost per second spent in the simulate state")]
    [SerializeField] private float _timePenaltyPerSecond = 10f;
    [SerializeField] private int _pointsPerModifier = 100;
    [SerializeField] private int _starBonus = 500;

    [Header("Grades")]
    [Tooltip("Min score for each grade")]
    [SerializeField]
    private List<GradeThreshold> _gradeThresholds = new List<GradeThreshold>
    {
        new GradeThreshold { grade = "A+", minimumScore = 1980 },
        new GradeThreshold { grade = "A",  minimumScore = 1900 },
        new GradeThreshold { grade = "B",  minimumScore = 1800 },
        new GradeThreshold { grade = "C",  minimumScore = 1400 },
        new GradeThreshold { grade = "D",  minimumScore = 1000 },
        new GradeThreshold { grade = "F",  minimumScore = 700  },
    };

    [System.Serializable]
    public struct GradeThreshold
    {
        public string grade;
        public int minimumScore;
    }

    public static LevelScoreManager Instance { get; private set; }

    private float _levelStartTime;
    private int _starsCollected = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        // Subscribe to game state event changes
        if (_stateManager != null)
        {
            _stateManager.OnGameStateChanged += HandleGameStateChanged;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from game state event changes
        if (_stateManager != null)
        {
            _stateManager.OnGameStateChanged -= HandleGameStateChanged;
        }
    }

    public void RecordInitialState()
    {
        _starsCollected = 0;
    }


    public void SoftReset()
    {
        HardReset();
    }


    public void HardReset()
    {
        _starsCollected = 0;
    }

    /// <summary>
    /// Increments the number collectables
    /// </summary>
    public void CollectableCollected()
    {
        _starsCollected++;
    }

    /// <summary>
    /// Calculates the final score, grade and updated high score
    /// </summary>
    /// <returns>A tuple containing the final score, grade string, and the high score</returns>
    public (int finalScore, string finalGrade, int highScore) StoreResults(int totalModifiersUsed)
    {
        float timeTaken = Time.time - _levelStartTime;

        (int timeScore, int modifierScore, int collectionScore, int totalScore, string grade) = CalculateResults(timeTaken, totalModifiersUsed);

        Debug.Log($"{gameObject.name}: Modifiers used: {totalModifiersUsed} | Collection Score: {collectionScore} | timeTaken: {timeTaken} | timeScore: {timeScore}");

        // Level index is set in the build settings
        string levelKey = $"Level_{SceneManager.GetActiveScene().buildIndex}_Highscore";
        string gradeKey = $"Level_{SceneManager.GetActiveScene().buildIndex}_Grade";
        // Playerprefs persists scores after closing game
        int savedHighScore = PlayerPrefs.GetInt(levelKey, 0);

        if (totalScore > savedHighScore)
        {
            PlayerPrefs.SetInt(levelKey, totalScore);
            PlayerPrefs.SetString(gradeKey, grade);
            PlayerPrefs.Save();
            savedHighScore = totalScore;
        }

        return (totalScore, grade, savedHighScore);
    }

    private (int timeScore, int modifierScore, int collectionScore, int totalScore, string grade) CalculateResults(float timeTaken, int totalModifiersUsed)
    {
        int timeScore = Mathf.Max(0, _baseScore - Mathf.RoundToInt(timeTaken * _timePenaltyPerSecond));
        int modifierScore = totalModifiersUsed * _pointsPerModifier;
        int collectionScore = _starsCollected * _starBonus;
        int totalScore = timeScore + modifierScore + collectionScore;
        string grade = EvaluateGrade(totalScore);

        return (timeScore, modifierScore, collectionScore, totalScore, grade);
    }

    private string EvaluateGrade(int score)
    {
        // note this assumes that the grade thresholds are from highest to lowest
        for (int i = 0; i < _gradeThresholds.Count; i++)
        {
            if (score >= _gradeThresholds[i].minimumScore)
            {
                return _gradeThresholds[i].grade;
            }
        }

        return "F-";
    }

    private void HandleGameStateChanged(GameState state)
    {
        // Start clock when player starts simulation
        if (state == GameState.Simulate)
        {
            _levelStartTime = Time.time;
        }
    }
}