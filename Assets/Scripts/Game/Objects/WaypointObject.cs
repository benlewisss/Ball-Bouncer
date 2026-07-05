using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class WaypointObject : MonoBehaviour, IResettable
{
    [Header("Configuration")]
    [SerializeField] private Vector3[] _localWaypoints;
    [SerializeField] private float _speed = 2f;
    [SerializeField] private float _waypointStopTime = 0f;
    [SerializeField] private bool _loop = true;

    [Tooltip("If enabled, this object will only move when it contacts another rigidbody")]
    [SerializeField] private bool _waitForTrigger = false;

    private Rigidbody2D _rigidbody2d;
    private Vector3[] _worldWaypoints;
    private int _currentWaypointIndex = 0;
    private Vector3 _initialPosition;

    private bool _waiting = false;
    private bool _triggered = false;

    private void Awake()
    {
        _rigidbody2d = GetComponent<Rigidbody2D>();
        RecordInitialState();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Trigger();
    }

    // Moving rigidbodies should be done in fixedupdate as it's tied to game physics timestep
    private void FixedUpdate()
    {
        if (_waitForTrigger && !_triggered) return;

        if (_worldWaypoints.Length == 0 || _waiting) return;

        Vector3 targetPosition = _worldWaypoints[_currentWaypointIndex];
        Vector3 newPosition = Vector3.MoveTowards(transform.position, targetPosition, _speed * Time.fixedDeltaTime);
        _rigidbody2d.MovePosition(newPosition);

        // We check if we reached the current waypoint with a small tolerance to allow for platform physics differences and floating point precision etc.
        if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
        {
            StartCoroutine(IncrementWaypointWithDelay(_waypointStopTime));
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (_localWaypoints == null || _localWaypoints.Length == 0)
        {
            return;
        }

        Gizmos.color = Color.orange;
        for (int i = 0; i < _localWaypoints.Length; i++)
        {
            Gizmos.DrawSphere(transform.position + _localWaypoints[i], 0.1f);
        }
    }

    public void RecordInitialState()
    {
        _initialPosition = transform.position;
        _worldWaypoints = new Vector3[_localWaypoints.Length];
        for (int i = 0; i < _localWaypoints.Length; i++)
        {
            _worldWaypoints[i] = _initialPosition + _localWaypoints[i];
        }
    }

    public void SoftReset()
    {
        HardReset();
    }

    public void HardReset()
    {
        StopAllCoroutines();
        _currentWaypointIndex = 0;
        _waiting = false;
        _triggered = false;
        _rigidbody2d.position = _initialPosition;
        transform.position = _initialPosition;
    }
    public void Trigger()
    {
        if (!_waitForTrigger || _triggered) return;
        _triggered = true;
    }

    public IEnumerator IncrementWaypointWithDelay(float delay)
    {
        _waiting = true;

        float elapsed = 0.0f;
        while (elapsed < delay)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }


        _currentWaypointIndex++;

        if (_currentWaypointIndex >= _worldWaypoints.Length)
        {
            if (_loop)
            {
                _currentWaypointIndex = 0;
            }
            else
            {
                _currentWaypointIndex = 0;
                _triggered = false;
            }
        }

        _waiting = false;
    }
}