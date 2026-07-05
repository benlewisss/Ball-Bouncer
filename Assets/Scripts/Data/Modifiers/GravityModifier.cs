using UnityEngine;

[CreateAssetMenu(fileName = "GravityModifier", menuName = "Modifiers/Gravity")]
public class GravityModifier : GenericModifierData
{
    [Range(-5f, 5f)]
    [SerializeField] private float _gravityMultipler = -1f;

    private void OnValidate()
    {
        if (Mathf.Approximately(_gravityMultipler, 0f))
        {
            _gravityMultipler = 0.001f;
        }
    }

    public override void Apply(InteractableObject target)
    {
        Rigidbody2D rigidbody = target.GetRigidbody();
        if (rigidbody != null)
        {
            rigidbody.gravityScale *= _gravityMultipler;
        }
    }

    public override void Revert(InteractableObject target)
    {
        Rigidbody2D rigidbody = target.GetRigidbody();
        if (rigidbody != null)
        {
            rigidbody.gravityScale /= _gravityMultipler;
        }
    }
}