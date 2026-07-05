using UnityEngine;

[CreateAssetMenu(fileName = "BouncyModifier", menuName = "Modifiers/Bounciness")]
public class BouncyModifier : GenericModifierData
{
    [Range(0, 4f)]
    [Tooltip("What percent of speed should be kept after a bounce")]
    // ForceRetention is used in InteractableObject to calculate bounces. We have to do this instead
    // of just setting bounciness over 1 to keep the physics more deterministic (bounciness over 1
    // causes all sorts of physics inconsistencies I thgnk because of floating point buildup)
    [SerializeField] public float ForceRetention = 1f;

    public override void Apply(InteractableObject target)
    {
        // Properties of materials can only be updated at runtime prior to a material
        // being applied to a collider, so we must update the properties first of a new
        // physics material instance before applying it to an InteractableObject collider.
        PhysicsMaterial2D material = target.GetInstancedMaterial();
        material.bounciness = 1f;
        target.SetMaterial(material);
    }

    public override void Revert(InteractableObject target)
    {
        target.RevertToOriginalMaterial();
    }
}