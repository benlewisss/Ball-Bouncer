using UnityEngine;

/// <summary>
/// Listens to global game state changes to show or hide the inventory UI
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class InventoryUIController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private GameStateManager _stateManager;

    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        // Subscribe to game state change events
        if (_stateManager != null)
        {
            _stateManager.OnGameStateChanged += HandleStateChanged;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe to game state change events
        if (_stateManager != null)
        {
            _stateManager.OnGameStateChanged -= HandleStateChanged;
        }
    }

    private void HandleStateChanged(GameState state)
    {
        bool isSetup = (state == GameState.Setup);
        _canvasGroup.alpha = isSetup ? 1f : 0f;
        _canvasGroup.interactable = isSetup;
        _canvasGroup.blocksRaycasts = isSetup;
    }
}