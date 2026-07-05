using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIAutoDrawer : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    [Tooltip("The actual UI panel containing the buttons that will slide up and down.")]
    [SerializeField] private RectTransform _targetDrawer;

    [Header("Configuration")]
    [Tooltip("The local position of the drawer when its fully visible")]
    [SerializeField] private Vector2 _shownAnchoredPosition = new Vector2(0, 0);
    [Tooltip("The local position of the drawer when fully hidden")]
    [SerializeField] private Vector2 _hiddenAnchoredPosition = new Vector2(0, -200);
    [SerializeField] private float _slideSpeed = 10f;

    [Header("Audio")]
    [SerializeField] private AudioCueData _hoverEnterSound;
    [SerializeField] private AudioCueData _hoverExitSound;
    [SerializeField] private AudioCueEventChannel _audioChannel;

    private Vector2 _currentTargetPosition;

    private void Start()
    {
        _currentTargetPosition = _hiddenAnchoredPosition;

        if (_targetDrawer != null)
        {
            _targetDrawer.anchoredPosition = _hiddenAnchoredPosition;
        }
    }

    private void Update()
    {
        if (_targetDrawer == null)
        {
            return;
        }

        // Check the distance to snap the drawer to its target position once it gets close.
        // We need to do this to stop the Lerp from hanging 
        if (Vector2.Distance(_targetDrawer.anchoredPosition, _currentTargetPosition) < 0.5f)
        {
            _targetDrawer.anchoredPosition = _currentTargetPosition;
            return;
        }

        // Lerp the position to slide it out smoothly
        _targetDrawer.anchoredPosition = Vector2.Lerp(
            _targetDrawer.anchoredPosition,
            _currentTargetPosition,
            Time.deltaTime * _slideSpeed
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _audioChannel?.RaiseEvent(_hoverEnterSound, Vector3.zero);
        _currentTargetPosition = _shownAnchoredPosition;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _audioChannel?.RaiseEvent(_hoverExitSound, Vector3.zero);
        _currentTargetPosition = _hiddenAnchoredPosition;
    }
}