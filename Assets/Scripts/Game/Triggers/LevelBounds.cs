using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LevelBounds : MonoBehaviour, IResettable
{
    [Header("Dependencies")]
    [SerializeField] private LevelEndEventChannel _levelEndChannel;
    [SerializeField] private GameStateManager _stateManager;

    [Header("Ball Configuration")]
    [Tooltip("Time ball can be out of bounds before losing")]
    [SerializeField] private float _outOfBoundsAllowance = 1.5f;

    [Header("Interactable Object Configuration")]
    [Tooltip("Time an interactable object can be out of bounds before it's removed. " +
        "Note that because a ball is an interactable object - " +
        "this number should be larger than the ball's allowance")]
    [SerializeField] private float _interactableDestructionAllowance = 3.0f;

    private bool _isBallOutOfBounds = false;
    private float _outOfBoundsTimer = 0f;

    // There can be multiple interactable objects out of bounds at any one time so we have to
    // keep track of the time each one was out of bounds invidiually.
    private record OutOfBoundsInteractable
    {
        public InteractableObject interactableObject;
        public float Timer;
    }

    private List<OutOfBoundsInteractable> _outOfBoundsInteractables = new List<OutOfBoundsInteractable>();

    private void Update()
    {
        if (_stateManager.CurrentState != GameState.Simulate) 
        {
            return;
        }

        // Ball out of bounds
        if (_isBallOutOfBounds)
        {
            _outOfBoundsTimer += Time.deltaTime;

            if (_outOfBoundsTimer >= _outOfBoundsAllowance)
            {
                if (_levelEndChannel != null)
                {
                    _levelEndChannel.RaiseEvent(false);
                }

                _isBallOutOfBounds = false;
                _outOfBoundsTimer = 0f;
            }
        }

        // Interactable object out of bounds
        // need to iterate backwards when removing from list in c# because they are shifted down one when element is removed
        for (int i = _outOfBoundsInteractables.Count - 1; i >= 0; i--)
        {
            OutOfBoundsInteractable outOfBoundsInteractable = _outOfBoundsInteractables[i];

            // If the object doesn't exist at this point anymore (e.g. was destroyed externally) then remove it from list
            if (outOfBoundsInteractable.interactableObject == null)
            {
                _outOfBoundsInteractables.RemoveAt(i);
                continue;
            }

            outOfBoundsInteractable.Timer += Time.deltaTime;

            if (outOfBoundsInteractable.Timer >= _interactableDestructionAllowance)
            {
                outOfBoundsInteractable.interactableObject.gameObject.SetActive(false);
                _outOfBoundsInteractables.RemoveAt(i);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        BallActor ball = collision.GetComponent<BallActor>();
        if (ball != null)
        {
            _isBallOutOfBounds = false;
            _outOfBoundsTimer = 0f;
            return;
        }

        InteractableObject interactable = collision.GetComponent<InteractableObject>();
        if (interactable != null)
        {
            for (int i = _outOfBoundsInteractables.Count - 1; i >= 0; i--)
            {
                if (_outOfBoundsInteractables[i].interactableObject == interactable)
                {
                    _outOfBoundsInteractables.RemoveAt(i);
                    break;
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        BallActor ball = collision.GetComponent<BallActor>();
        if (ball != null)
        {
            _isBallOutOfBounds = true;
            return;
        }

        InteractableObject interactable = collision.GetComponent<InteractableObject>();
        if (interactable != null)
        {
            // I think you can do this with LINQ but I don't really understand it and this is more readable
            bool isAlreadyTracked = false;
            for (int i = 0; i < _outOfBoundsInteractables.Count; i++)
            {
                if (_outOfBoundsInteractables[i].interactableObject == interactable)
                {
                    isAlreadyTracked = true;
                    break;
                }
            }

            if (!isAlreadyTracked)
            {
                _outOfBoundsInteractables.Add(new OutOfBoundsInteractable
                {
                    interactableObject = interactable,
                    Timer = 0f
                });
            }
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
        _isBallOutOfBounds = false;
        _outOfBoundsTimer = 0f;
        _outOfBoundsInteractables.Clear();
    }
}