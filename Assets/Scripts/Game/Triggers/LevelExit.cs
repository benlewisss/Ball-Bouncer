using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LevelExit : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private LevelEndEventChannel _levelEndChannel;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        BallActor ball = collision.GetComponent<BallActor>();

        if (ball != null)
        {
            if (_levelEndChannel != null)
            {
                _levelEndChannel.RaiseEvent(true);
            }
        }
    }
}