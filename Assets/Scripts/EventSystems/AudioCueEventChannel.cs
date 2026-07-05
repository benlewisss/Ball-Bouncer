using System;
using UnityEngine;

/// <summary>
/// Event channel for SFX audio play requests
/// </summary>
[CreateAssetMenu(fileName = "AudioCueEventChannel", menuName = "Events/AudioCueEventChannel")]
public class AudioCueEventChannel : ScriptableObject
{
    /// <summary>
    /// Event triggered when SFX are requested
    /// </summary>
    public event Action<AudioCueData, Vector3, bool> OnAudioPlayRequested;

    /// <summary>
    /// Broadcasts a request to play audio at a location
    /// </summary>
    /// <param name="audioCue">The audio data to play</param>
    /// <param name="position">The world location to play the sound</param>
    /// <param name="singleInstance">Only play one instance of this sound at this location</param>
    public void RaiseEvent(AudioCueData audioCue, Vector3 position, bool singleInstance=false)
    {
        if (audioCue != null)
        {
            OnAudioPlayRequested?.Invoke(audioCue, position, singleInstance);
        }
    }
}