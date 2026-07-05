using System.Collections;
using UnityEngine;

/// <summary>
/// Credit to @Thomas Friday (https://www.youtube.com/watch?v=BQGTdRhGmE4), from which inspiration 
/// and code was taken for this method.
/// </summary>
public class CameraShake : MonoBehaviour
{
    [SerializeField] private AnimationCurve _curve;

    /// <summary>
    /// Shakes the camera over a specified duration using an animation curve to determine intensity.
    /// Must be called as part of a coroutine.
    /// </summary>
    /// <param name="duration">Time in seconds the shake effect should last</param>
    public IEnumerator Shake(float duration)
    {
        // Uuse localPosition instead of position so shake works even if the camera is moving already
        Vector3 originalPosition = transform.localPosition;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float magnitude = _curve.Evaluate(elapsed / duration);
            transform.localPosition = originalPosition + (Random.insideUnitSphere * magnitude);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPosition;
    }
}