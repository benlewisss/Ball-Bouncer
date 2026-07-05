using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CollectableObject : MonoBehaviour, IResettable
{
    [Header("Audio")]
    [SerializeField] private AudioCueData _collectSound;
    [SerializeField] private AudioCueEventChannel _audioChannel;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            _audioChannel?.RaiseEvent(_collectSound, transform.position);

            if (LevelScoreManager.Instance != null)
            {
                LevelScoreManager.Instance.CollectableCollected();
            }

            // Disable the game object instead of destroying it so it can be reset later
            gameObject.SetActive(false);
        }
    }

    public void RecordInitialState()
    {
    }

    public void SoftReset()
    {
        HardReset();
    }

    public void HardReset()
    {
        gameObject.SetActive(true);
    }
}