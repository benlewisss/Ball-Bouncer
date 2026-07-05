using UnityEngine;
public class AutoAligner : MonoBehaviour, IResettable
{
    [SerializeField] private Transform _alignPoint;
    private bool _aligned = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_aligned) return;
        if (collision.GetComponent<BallActor>() == null) return;

        Rigidbody2D rigidbody2d = collision.GetComponent<Rigidbody2D>();
        if (rigidbody2d == null) return;

        _aligned = true;

        rigidbody2d.linearVelocity = Vector2.zero;
        rigidbody2d.angularVelocity = 0f;
        rigidbody2d.position = _alignPoint.position;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponent<BallActor>() == null) return;
        _aligned = false;
    }

    private void OnDrawGizmos()
    {
        if (_alignPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(_alignPoint.position, 0.375f);
    }

    public void RecordInitialState()
    {
    }

    public void SoftReset()
    {
        _aligned = false;
    }

    public void HardReset()
    {
        SoftReset();
    }
}