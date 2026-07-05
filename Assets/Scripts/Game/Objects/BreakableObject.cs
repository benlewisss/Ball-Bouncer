using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BreakableObject : MonoBehaviour, IResettable
{
    [Header("Configuration")]
    [Tooltip("The minimum velocity of the ball to break this object")]
    [SerializeField] private float _breakThreshold = 5f;
    [Tooltip("The percent of the velocity the ball should keep upon breaking through the wall")]
    [Range(0f, 1f)]
    [SerializeField] private float _velocityRetention = 0.6f;

    [Header("Effects")]
    [SerializeField] private ParticleSystem _breakParticles;

    [Header("Audio")]
    [SerializeField] private AudioCueData _breakSound;
    [SerializeField] private AudioCueEventChannel _audioChannel;

    private SpriteRenderer _spriteRenderer;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // I opted to instead have a trigger surrounding the breakable object that breaks the block before the ball even touches it,
    // so the ball never actually collides with the wall - this avoids all sorts of annoying physics bugs in terms of slowing the ball down etc.
    // This is what necessitates VelocityCache in the ballActor
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Don't want other objects breaking the wall
        if (collision.CompareTag("Player"))
        {
            Rigidbody2D rigidbody2d = collision.attachedRigidbody;
            BallActor ballActor = rigidbody2d != null ? rigidbody2d.GetComponent<BallActor>() : null;

            if (ballActor != null && rigidbody2d != null)
            {
                Vector2 incomingVelocity = ballActor.VelocityCache;

                if (incomingVelocity.magnitude >= _breakThreshold)
                {
                    Debug.Log($"<color=orange>BREAKABLE OBJECT</color>: {gameObject.name} broken, force: {incomingVelocity.magnitude}");
                    rigidbody2d.linearVelocity = incomingVelocity * _velocityRetention;
                    Break(incomingVelocity);
                }
            }
        }
    }

    public void RecordInitialState()
    {
    }

    public void SoftReset()
    {
        gameObject.SetActive(true);
    }

    public void HardReset()
    {
        SoftReset();
    }

    private void Break(Vector2 impactVelocity)
    {
        if (_breakParticles != null)
        {
            // Calculate the impactAngle based on the impact vector to rotate the particle system
            float impactAngle = Mathf.Atan2(impactVelocity.y, impactVelocity.x) * Mathf.Rad2Deg;
            Quaternion impactRotation = Quaternion.Euler(0f, 0f, impactAngle);
            Debug.Log($"<color=orange>BREAKABLE OBJECT</color>: {gameObject.name} particles: Impact Vector: {impactVelocity} | Particle angle: {impactAngle}");

            // Instantiate a new copy of the particle system prefab
            ParticleSystem spawnedParticles = Instantiate(_breakParticles, transform.position, impactRotation);

            if (_spriteRenderer != null)
            {
                ParticleSystem.MainModule mainModule = spawnedParticles.main;
                mainModule.startColor = _spriteRenderer.color;
            }
        }

        _audioChannel?.RaiseEvent(_breakSound, transform.position);

        // Disable so we can reset later
        gameObject.SetActive(false);
    }
}