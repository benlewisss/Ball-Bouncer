using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Automatically plays a sound when the attached UI element is clicked
/// </summary>
public class UIAudioEmitter : MonoBehaviour, IPointerClickHandler
{
    [Header("Audio")]
    [SerializeField] private AudioCueData _clickSound;
    [SerializeField] private AudioCueEventChannel _audioChannel;

    public void OnPointerClick(PointerEventData eventData)
    {
        _audioChannel?.RaiseEvent(_clickSound, Vector3.zero);
    }
}