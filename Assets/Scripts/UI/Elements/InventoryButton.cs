using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button), typeof(Image))]
public class InventoryButton : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private GenericModifierData _assignedModifier;
    [SerializeField] private ModifierSelectionEventChannel _selectionChannel;

    [Header("UI Text Box References")]
    [SerializeField] private TextMeshProUGUI _countText;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _descriptionText;

    [Header("Config")]
    [SerializeField] private Color _selectedColor = Color.gray;
    [SerializeField] private Color _depletedColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    private Color _defaultColor;
    private Button _buttonComponent;
    private Image _buttonImage;
    private bool _isSelected = false;

    private void Awake()
    {
        _buttonComponent = GetComponent<Button>();
        _buttonImage = GetComponent<Image>();

        _defaultColor = _buttonImage.color;

        // Bind button
        _buttonComponent.onClick.AddListener(OnButtonClicked);
    }

    private void OnEnable()
    {
        // Subscribe to modifier selection events
        if (_selectionChannel != null)
        {
            _selectionChannel.OnModifierSelected += HandleSelectionChanged;
        }

        if (_assignedModifier != null)
        {
            // Make text and color match the assigned modifier for this button
            if (_nameText != null)
            {
                _nameText.text = _assignedModifier.modifierName;
                _nameText.color = _assignedModifier.modifierColor;
            }

            if (_descriptionText != null)
            {
                _descriptionText.text = _assignedModifier.effectDescription;
                _descriptionText.color = _assignedModifier.modifierColor;
            }

            if (_countText != null)
            {
                _countText.color = _assignedModifier.modifierColor;
            }

            // Get InventoryManager and subscribe to inventory change events directly from it
            // (because it's a singleton and this is the only place where inventory changes need to be observed,
            // there was no need for a dedicated event channel).
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnInventoryChanged += HandleInventoryChanged;
                UpdateVisuals(InventoryManager.Instance.GetCount(_assignedModifier));
            }
        }
    }

    private void OnDisable()
    {
        // Unsubscribe to modifier selection events
        if (_selectionChannel != null)
        {
            _selectionChannel.OnModifierSelected -= HandleSelectionChanged;
        }

        // Unsubscribe from inventory change events
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= HandleInventoryChanged;
        }
    }

    private void HandleInventoryChanged(GenericModifierData modifiedData, int newCount)
    {
        if (modifiedData == _assignedModifier)
        {
            UpdateVisuals(newCount);
        }
    }

    private void UpdateVisuals(int currentCount)
    {
        if (_countText != null)
        {
            _countText.text = currentCount.ToString();
        }

        if (currentCount <= 0)
        {
            _buttonComponent.interactable = false;
            _buttonImage.color = _depletedColor;

            if (_isSelected)
            {
                _selectionChannel.RaiseEvent(null);
            }
        }
        else
        {
            _buttonComponent.interactable = true;
            _buttonImage.color = _isSelected ? _selectedColor : _defaultColor;
        }
    }

    private void HandleSelectionChanged(GenericModifierData selectedModifier)
    {
        _isSelected = (selectedModifier == _assignedModifier);

        // Only update the color if the inventory is not depleted
        if (InventoryManager.Instance != null && InventoryManager.Instance.GetCount(_assignedModifier) > 0)
        {
            _buttonImage.color = _isSelected ? _selectedColor : _defaultColor;
        }
    }

    private void OnButtonClicked()
    {
        if (_assignedModifier != null && _selectionChannel != null)
        {
            if (InventoryManager.Instance != null && InventoryManager.Instance.GetCount(_assignedModifier) <= 0)
            {
                return;
            }

            if (_isSelected)
            {
                _selectionChannel.RaiseEvent(null);
            }
            else
            {
                _selectionChannel.RaiseEvent(_assignedModifier);
            }
        }
    }
}