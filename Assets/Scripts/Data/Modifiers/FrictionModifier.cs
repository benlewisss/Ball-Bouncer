using UnityEngine;

[CreateAssetMenu(fileName = "FrictionModifier", menuName = "Modifiers/Friction")]
public class FrictionModifier : GenericModifierData
{
    [Range(0f, 20f)]
    [SerializeField] private float _frictionMultipler = 0f;

    private void OnValidate()
    {
        if (Mathf.Approximately(_frictionMultipler, 0f))
        {
            _frictionMultipler = 0.001f;
        }
    }

    public override void Apply(InteractableObject target)
    {
        PhysicsMaterial2D material = target.GetInstancedMaterial();
        material.friction *= _frictionMultipler;
        target.SetMaterial(material);
    }

    public override void Revert(InteractableObject target)
    {
        target.RevertToOriginalMaterial();
    }
}