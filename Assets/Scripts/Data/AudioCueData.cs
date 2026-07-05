using UnityEngine;

/// <summary>
/// A container for sound effects.
/// </summary>
[CreateAssetMenu(fileName = "AudioCueData", menuName = "SFX/AudioCue")]
public class AudioCueData : ScriptableObject
{
    [Tooltip("Clips associated with this audio cue, will randomly play one")]
    [SerializeField] private AudioClip[] _clips;

    [Range(0f, 1f)]
    [SerializeField] private float _volume = 1f;

    [Range(0.1f, 3f)]
    [SerializeField] private float _minPitch = 0.9f;

    [Range(0.1f, 3f)]
    [SerializeField] private float _maxPitch = 1.1f;

    public AudioClip GetRandomClip()
    {
        if (_clips == null || _clips.Length == 0)
        {
            return null;
        }

        int randomIndex = Random.Range(0, _clips.Length);
        return _clips[randomIndex];
    }

    public float GetVolume()
    {
        return _volume;
    }

    public float GetRandomPitch()
    {
        return Random.Range(_minPitch, _maxPitch);
    }
}