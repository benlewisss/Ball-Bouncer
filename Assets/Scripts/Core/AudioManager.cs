using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manager that listens for audio events and play's them were requested
/// </summary>
// DefaultExecutionOrder makes sure this script Awake/Start method run before the other scripts (which default to 0).
// Need to do this for singleton managers as other scripts may rely on them during initialisation.
[DefaultExecutionOrder(-10)]
public class AudioManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private AudioCueEventChannel _audioCueChannel;

    public static AudioManager Instance { get; private set; }
    private List<AudioSource> _activeSources = new List<AudioSource>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        if (_audioCueChannel != null)
        {
            _audioCueChannel.OnAudioPlayRequested += HandleAudioPlayRequested;
        }
    }

    private void OnDisable()
    {
        if (_audioCueChannel != null)
        {
            _audioCueChannel.OnAudioPlayRequested -= HandleAudioPlayRequested;
        }
    }

    private void HandleAudioPlayRequested(AudioCueData audioCue, Vector3 position, bool singleInstance)
    {
        if (audioCue == null) return;

        AudioClip audioClip = audioCue.GetRandomClip();
        if (audioClip == null) return;

        // When singleInstance is true, we only play one instance of a particular audioCue if within a close
        // radius of another audio source playing that same audioCue. Useful for collisions.
        if (singleInstance)
        {
            for (int i = 0; i < _activeSources.Count; i++)
            {
                AudioSource proximitySource = _activeSources[i];

                // If different sounds or at different locations, then ignore
                // TODO This won't work if we assign multiple audio clips for a particular AudioCuteData, because the clips signature will no
                // longer be equatable for a given AudioCuteData (can be different clips), so would need to solve this.
                if (proximitySource.clip != audioClip) continue;
                if (Vector3.Distance(position, proximitySource.transform.position) > 2.5f) continue;
                return;
            }
        }

        GameObject audioSourceObject = new GameObject("AudioSFX");
        audioSourceObject.transform.position = position;
                
        float pitch = audioCue.GetRandomPitch();

        AudioSource audioSource = audioSourceObject.AddComponent<AudioSource>();
        audioSource.clip = audioClip;
        audioSource.spatialBlend = 0f;
        audioSource.pitch = pitch;
        audioSource.volume = audioCue.GetVolume();

        _activeSources.Add(audioSource);
        audioSource.Play();

        // Have to let the source play the sound before removing, and it's length is influenced by it's pitch
        StartCoroutine(RemoveAudioSource(audioSource, audioClip.length / pitch));
    }

    public IEnumerator RemoveAudioSource(AudioSource audioSource, float delay)
    {
        float elapsed = 0.0f;
        while (elapsed < delay)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        _activeSources.Remove(audioSource);
        Destroy(audioSource.gameObject);
    }
}