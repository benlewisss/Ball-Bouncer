using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BallActor : MonoBehaviour, IResettable
{
    [Header("Dependencies")]
    [SerializeField] private LevelEndEventChannel _levelEndChannel;

    [Header("Loss Condition Configuration")]
    [Tooltip("The velocity below which the ball is considered stationary")]
    [SerializeField] private float _stationaryThreshold = 0.1f;
    [Tooltip("How many seconds the ball must be stationary to trigger a loss")]
    [SerializeField] private float _timeToFail = 2.0f;

    public Vector2 VelocityCache { get; private set; }

    private Rigidbody2D _rigidbody2D;
    private Vector2 _initialPosition;
    private Quaternion _initialRotation;

    private TrailRenderer _trailRenderer;
    private Color _trailRendererOriginalStartColor;

    private float _stationaryTimer = 0f;

    private void Awake()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _trailRenderer = GetComponent<TrailRenderer>();

        _trailRendererOriginalStartColor = _trailRenderer.startColor;
    }

    private void FixedUpdate()
    {
        if (_rigidbody2D != null && _rigidbody2D.simulated)
        {
            VelocityCache = _rigidbody2D.linearVelocity;
        }
    }

    private void Update()
    {
        // Only check for a loss if physics is enabled for the ball
        if (!_rigidbody2D.simulated)
        {
            _stationaryTimer = 0f;
            return;
        }

        if (_rigidbody2D.linearVelocity.magnitude < _stationaryThreshold)
        {
            _stationaryTimer += Time.deltaTime;

            if (_stationaryTimer >= _timeToFail)
            {
                if (_levelEndChannel != null)
                {
                    _levelEndChannel.RaiseEvent(false);
                }
                _stationaryTimer = 0f;
            }
        }
        else
        {
            // If the ball is faster than the threshold then reset the timer
            _stationaryTimer = 0f;
        }
    }

    public void RecordInitialState()
    {
        _initialPosition = transform.position;
        _initialRotation = transform.rotation;
    }

    public void SoftReset()
    {
        HardReset();
    }

    public void HardReset()
    {
        _rigidbody2D.linearVelocity = Vector2.zero;
        _rigidbody2D.angularVelocity = 0f;

        transform.position = _initialPosition;
        transform.rotation = _initialRotation;

        _stationaryTimer = 0f;

        // Clear the trail renderer so it doesn't draw a massive line from where it was to the start
        if (_trailRenderer != null)
        {
            _trailRenderer.startColor = _trailRendererOriginalStartColor;
            _trailRenderer.Clear();
        }
    }

    /// <summary>
    /// Enables or disables physics simulation for the ball. If this is enabled the ball will actively check for a loss condition.
    /// </summary>
    public void SetSimulated(bool isSimulated)
    {
        _rigidbody2D.simulated = isSimulated;
    }
}