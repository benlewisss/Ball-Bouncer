using UnityEngine;

[CreateAssetMenu(fileName = "StickyModifier", menuName = "Modifiers/Stickyness")]
public class StickyModifier : GenericModifierData
{
    [Range(1f, 30f)]
    [SerializeField] private float _breakFreeSpeed = 4f;
    [Range(1f, 30f)]
    [SerializeField] private float _damping = 8f;

    public override void Apply(InteractableObject target)
    {
        StickyEffect effect = target.gameObject.AddComponent<StickyEffect>();
        effect.BreakFreeSpeed = _breakFreeSpeed;
        effect.Damping = _damping;

        PhysicsMaterial2D material = target.GetInstancedMaterial();
        material.friction += 2f;
        material.bounciness = 0f;
        target.SetMaterial(material);
    }

    public override void Revert(InteractableObject target)
    {
        StickyEffect effect = target.GetComponent<StickyEffect>();
        if (effect != null) Destroy(effect);

        target.RevertToOriginalMaterial();
    }
}