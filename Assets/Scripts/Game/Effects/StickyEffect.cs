using UnityEngine;

public class StickyEffect : MonoBehaviour
{
    public float BreakFreeSpeed { get; set; } = 4f;
    public float Damping { get; set; } = 8f;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Rigidbody2D rigidbody2d = collision.rigidbody;
        if (rigidbody2d == null || rigidbody2d.bodyType == RigidbodyType2D.Static) return;

        float speed = rigidbody2d.linearVelocity.magnitude;

        // Get the tangent to the collision point, and get the component of velocity in that direction
        // with the dot product, then update the velocity to match.
        // This makes the ball slide down a sticky surface rather than bounce off it because we remove 
        // all velocity that isn't tangential to the plane it collides with off.
        Vector2 contactNormal = collision.GetContact(0).normal;
        Vector2 tangent = Vector2.Perpendicular(contactNormal);
        float tangentSpeed = Vector2.Dot(rigidbody2d.linearVelocity, tangent);

        if (speed <= BreakFreeSpeed)
        {
            rigidbody2d.linearVelocity = tangent * tangentSpeed;
        }
        else
        {
            return;
        }

    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // Stickyness applies to both self and anything touching it
        ApplyDrag(GetComponent<Rigidbody2D>(), collision.GetContact(0).point);
        ApplyDrag(collision.rigidbody, collision.GetContact(0).point);
    }

    private void ApplyDrag(Rigidbody2D rigidbody2d, Vector2 contactPoint)
    {
        if (rigidbody2d == null || rigidbody2d.bodyType == RigidbodyType2D.Static) return;

        float speed = rigidbody2d.linearVelocity.magnitude;
        if (rigidbody2d.linearVelocity.magnitude < 0.05)
        {
            rigidbody2d.linearVelocity = Vector2.zero;
            return;
        }

        // The effect of sticky surface is at maximum when object is at rest on it, and as it goes towards break free speed the drag reduces
        float stickyness = Mathf.Clamp01(1f - (speed / (BreakFreeSpeed * 2f)));
        float stickynessDamping = Damping * stickyness;

        // Apply a force opposing the current velocity (i.e. drag/damping)
        rigidbody2d.linearVelocity *= Mathf.Max(0f, 1f - stickynessDamping * Time.fixedDeltaTime);

        // pull towards the contact point where the objects meet
        rigidbody2d.AddForce((contactPoint - rigidbody2d.position).normalized * stickynessDamping, ForceMode2D.Force);

        // counteract gravity (so ball can stick to the ceiling correctly)
        Vector2 inverseGravity = -Physics2D.gravity * rigidbody2d.mass * rigidbody2d.gravityScale * stickyness;
        rigidbody2d.AddForce(inverseGravity, ForceMode2D.Force);
    }
}