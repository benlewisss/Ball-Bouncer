using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D;

[RequireComponent(typeof(Collider2D))]
public class InteractableObject : MonoBehaviour, IResettable
{
    [Header("Dependencies")]
    [SerializeField] private ModifierSelectionEventChannel _selectionChannel;

    [Header("Configuration")]
    [Tooltip("A list of modifiers that can be applied to this object - will allow any modifier if empty.")]
    [SerializeField] private List<GenericModifierData> _explicitlyAllowedModifiers = new List<GenericModifierData>();
    [SerializeField] private float _maxVelocity = 25f;

    [Header("Highlight Configuration")]
    [SerializeField] private Color _outlineHighlightColor = Color.salmon;
    [SerializeField] private float _outlineHighlightThickness = 0.05f;
    [SerializeField] private int _outlineHighlightSortingOrder = 20;

    [Tooltip("Configuration for the default border that appears around modifiable objects.")]
    [Header("Border Configuration")]
    [SerializeField] private Color _borderColor = Color.white;
    [SerializeField] private float _borderOutlineThickness = 0.025f;
    [SerializeField] private int _borderOutlineSortingOrder = 20;

    [Header("Effects")]
    [SerializeField] private ParticleSystem _modifierParticles;
    [SerializeField] private TrailRenderer _trailRenderer;
    [SerializeField] private float _particleBorderPadding = 0.2f;

    [Header("Audio")]
    [SerializeField] private AudioCueData _impactSound;
    [SerializeField] private AudioCueData _bounceSound;
    [SerializeField] private AudioCueEventChannel _audioChannel;
    [Tooltip("Min velocity required to trigger SFX")]
    [SerializeField] private float _minimumImpactVelocity = 1.5f;

    private Collider2D _collider2d;
    private Rigidbody2D _rigidbody2d;

    private SpriteRenderer[] _childSpriteRenderers;
    private LineRenderer _highlightRenderer;
    private LineRenderer _borderRenderer;

    private Vector3 _originalPosition;
    private Quaternion _originalRotation;
    private PhysicsMaterial2D _originalPhysicsMaterial;
    private PhysicsMaterial2D _instancedPhysicsMaterial;

    private Material _defaultVisualMaterial;
    private Color _defaultColor;

    private struct AppliedModifierRecord
    {
        public GenericModifierData ModifierData;
        public bool WasPaidFor;
    }

    private Stack<AppliedModifierRecord> _appliedModifiers = new Stack<AppliedModifierRecord>();

    // Whether this object is currently a valid target for a selected modifier (used to indiciate if it should be highlighted)
    private bool _isCurrentlyValidTarget = false;
    private GenericModifierData _lastSelectedModifier = null;

    private void Awake()
    {
        _collider2d = GetComponent<Collider2D>();
        _rigidbody2d = GetComponent<Rigidbody2D>();
        _childSpriteRenderers = GetComponentsInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        ConfigureParticleShape();
        // Highlight outline has to be in start not awake to ensure that the parent CompositeCollider2D has processed it's children
        _highlightRenderer = OutlineGenerator("HighlightOutline", _outlineHighlightColor, _outlineHighlightThickness, _outlineHighlightSortingOrder);
        _borderRenderer = OutlineGenerator("ModifierBorderOutline", _borderColor, _borderOutlineThickness, _borderOutlineSortingOrder);
    }

    private void OnEnable()
    {
        // Subscribe to modifier selection events
        if (_selectionChannel != null)
        {
            _selectionChannel.OnModifierSelected += CheckValidModifierTarget;
        }
    }

    private void OnDisable()
    {
        // Unubscribe to modifier selection events
        if (_selectionChannel != null)
        {
            _selectionChannel.OnModifierSelected -= CheckValidModifierTarget;
        }
    }

    private void OnDestroy()
    {
        if (_instancedPhysicsMaterial != null)
        {
            Destroy(_instancedPhysicsMaterial);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"<color=green>COLLISION</color>: {gameObject.name} collided with {collision.gameObject.name} | force: {collision.relativeVelocity.magnitude}");

        if (collision.relativeVelocity.magnitude < _minimumImpactVelocity)
        { 
            return; 
        }

        // Specific impact sounds depending on the surface of this object
        if (GetAppliedModifierCount() > 0)
        {
            BouncyModifier bouncyModifier = GetActiveModifiers().OfType<BouncyModifier>().FirstOrDefault();
            if (bouncyModifier != null)
            {
                // Things don't bounce off of sticky things
                if (collision.gameObject.GetComponent<StickyEffect>() != null)
                {
                    _audioChannel?.RaiseEvent(_impactSound, collision.GetContact(0).point, true);
                    return;
                }

                // Handle bounce modifier force retention above 1 - doing this manually rather than using bounciness > 1 is more deterministic (I am not
                // actually fully certain why, but this solved my problems with bouncing objects clipping - presumably it's a difference in how the
                // physics engine calculates velocity from bounciness).
                Rigidbody2D colliderRigidbody2d = collision.rigidbody;
                if (colliderRigidbody2d != null && colliderRigidbody2d.bodyType == RigidbodyType2D.Dynamic)
                {
                    colliderRigidbody2d.linearVelocity *= bouncyModifier.ForceRetention;

                }

                _audioChannel?.RaiseEvent(_bounceSound, collision.GetContact(0).point, true);
                return;
            }
        }

        _audioChannel?.RaiseEvent(_impactSound, collision.GetContact(0).point, true);
    }

    private void FixedUpdate()
    {
        // Clamping the max velocity to avoid clipping issues (especially with bouncy objects)
        if (_rigidbody2d != null && _rigidbody2d.simulated)
        {
            if (_rigidbody2d.linearVelocity.magnitude > _maxVelocity)
            {
                _rigidbody2d.linearVelocity = Vector2.ClampMagnitude(_rigidbody2d.linearVelocity, _maxVelocity);
            }
        }
    }

    /// <summary>
    /// Returns whether this interactable object can accept the specified modifier
    /// </summary>
    /// <param name="modifier">The modifier data</param>
    /// <param name="isPaidFor">Whether this modifier must be paid for (i.e. consumed from inventory)</param>
    /// <returns>True if the modifier can be applied, false otherwise</returns>
    public bool CanAcceptModifier(GenericModifierData modifier, bool isPaidFor = true)
    {
        if (modifier == null) return false;

        // Don't allow a modifier to be applied if one of same type is already applied
        foreach (AppliedModifierRecord record in _appliedModifiers)
        {
            if (record.ModifierData == modifier) return false;
        }

        // Can't accept this modifier if it's paid for and there isn't any in the inventory
        if (isPaidFor && InventoryManager.Instance != null && !InventoryManager.Instance.HasModifier(modifier)) return false;

        // Modifier must be in explicitly allowed list (if there is anything in the list)
        if (_explicitlyAllowedModifiers.Count > 0 && !_explicitlyAllowedModifiers.Contains(modifier)) return false;

        return true;
    }

    /// <summary>
    /// Consumes the modifier from the inventory, applies the effect, and updates the visuals
    /// </summary>
    /// <param name="modifier">The modifier data</param>
    /// <param name="isPaidFor">Whether this modifier must be paid for (i.e. consumed from inventory)</param>
    public void ApplyModifier(GenericModifierData modifier, bool isPaidFor = true)
    {
        // Only consume from inventory if it's paid for
        if (isPaidFor && InventoryManager.Instance != null)
        {
            if (!InventoryManager.Instance.ConsumeModifier(modifier))
            {
                return;
            }
        }

        // Only allow two modifiers to be applied to an object.
        // This always replaces the latest modifier if multiple are applied, meaning the base modifier (the one applied by a player)
        // stays, and any applied by the world will be replaced.
        // TODO This logic works but is not tied in tightly with how PlayerInteraction handles selection and application of modifiers
        if (_appliedModifiers.Count >= 2)
        {
            RevertLastModifier();
        }

        _appliedModifiers.Push(new AppliedModifierRecord
        {
            ModifierData = modifier,
            WasPaidFor = isPaidFor
        });

        modifier.Apply(this);
        UpdateVisualState();
    }

    /// <summary>
    /// Reverts the most recently applied modifier on this object and refunds it to the inventory
    /// </summary>
    public void RevertLastModifier()
    {
        if (_appliedModifiers.Count > 0)
        {
            AppliedModifierRecord appliedModifier = _appliedModifiers.Pop();
            appliedModifier.ModifierData.Revert(this);

            if (appliedModifier.WasPaidFor && InventoryManager.Instance != null)
            {
                InventoryManager.Instance.RefundModifier(appliedModifier.ModifierData);
            }

            // instance a new physics material for each modifier applied
            if (_instancedPhysicsMaterial != null)
            {
                Destroy(_instancedPhysicsMaterial);
                _instancedPhysicsMaterial = null;
            }

            RevertToOriginalMaterial();
            UpdateVisualState();
        }
    }

    /// <summary>
    /// Reverts all modifiers on this object
    /// </summary>
    public void RevertAllModifiers()
    {
        while (_appliedModifiers.Count > 0)
        {
            RevertLastModifier();
        }
    }

    /// <summary>
    /// Get all the active modifiers on this object
    /// </summary>
    public IEnumerable<GenericModifierData> GetActiveModifiers()
    {
        foreach (AppliedModifierRecord record in _appliedModifiers)
        {
            yield return record.ModifierData;
        }
    }

    /// <summary>
    /// Returns a fresh new instance of the physics material for this object
    /// </summary>
    public PhysicsMaterial2D GetInstancedMaterial()
    {
        if (_instancedPhysicsMaterial != null)
        {
            return _instancedPhysicsMaterial;
        }

        // Instantiate creates a unique copy so modifying it won't affect other objects sharing the original material
        _instancedPhysicsMaterial = _originalPhysicsMaterial == null ? new PhysicsMaterial2D("InstancedMaterial") : Instantiate(_originalPhysicsMaterial);
        return _instancedPhysicsMaterial;
    }

    /// <summary>
    /// Sets the physics material for this objects collider
    /// </summary>
    public void SetMaterial(PhysicsMaterial2D newMaterial)
    {
        _collider2d.sharedMaterial = newMaterial;
    }

    /// <summary>
    /// Revert the physics material for this objects collider to it's original material
    /// </summary>
    public void RevertToOriginalMaterial()
    {
        _collider2d.sharedMaterial = _originalPhysicsMaterial;
    }

    /// <summary>
    /// Returns the rigidbody component for this interactable object
    /// </summary>
    public Rigidbody2D GetRigidbody()
    {
        return _rigidbody2d;
    }

    public void RecordInitialState()
    {
        _originalPhysicsMaterial = _collider2d.sharedMaterial;

        if (_childSpriteRenderers != null && _childSpriteRenderers.Length > 0)
        {
            _defaultVisualMaterial = _childSpriteRenderers[0].sharedMaterial;
            _defaultColor = _childSpriteRenderers[0].color;
        }

        _originalPosition = transform.position;
        _originalRotation = transform.rotation;
    }

    /// <summary>
    /// Restores the object's starting position but retains any paid-for modifiers
    /// </summary>
    public void SoftReset()
    {
        Debug.Log($"<color=magenta>{gameObject.name}: Soft Reset!</color>");
        gameObject.SetActive(true);

        List<AppliedModifierRecord> modifiersToKeep = new List<AppliedModifierRecord>();

        while (_appliedModifiers.Count > 0)
        {
            AppliedModifierRecord appliedModifier = _appliedModifiers.Pop();
            appliedModifier.ModifierData.Revert(this);

            // Only keep modifiers the player actually applied manually and that were taken from the inventory
            if (appliedModifier.WasPaidFor)
            {
                modifiersToKeep.Insert(0, appliedModifier);
            }
        }

        _appliedModifiers.Clear();
        RevertToOriginalMaterial();

        if (_instancedPhysicsMaterial != null)
        {
            Destroy(_instancedPhysicsMaterial);
            _instancedPhysicsMaterial = null;
        }

        // Reset location
        transform.position = _originalPosition;
        transform.rotation = _originalRotation;
        // Reset mommentup if its not already static
        if (_rigidbody2d != null && _rigidbody2d.bodyType != RigidbodyType2D.Static)
        {
            _rigidbody2d.linearVelocity = Vector2.zero;
            _rigidbody2d.angularVelocity = 0f;
            _rigidbody2d.position = _originalPosition;
            _rigidbody2d.rotation = _originalRotation.eulerAngles.z;
        }

        // Re-apply all the paid for modifiers
        foreach (var modifierToApply in modifiersToKeep)
        {
            _appliedModifiers.Push(modifierToApply);
            Debug.Log($"Reapply modifier: {modifierToApply.ModifierData.name}");
            modifierToApply.ModifierData.Apply(this);
        }

        UpdateVisualState();
    }

    public void HardReset()
    {
        Debug.Log($"<color=purple>{gameObject.name}: Hard Reset!</color>");
        gameObject.SetActive(true);
        RevertAllModifiers();
        RevertToOriginalMaterial();

        _isCurrentlyValidTarget = false;
        UpdateVisualState();

        if (_instancedPhysicsMaterial != null)
        {
            Destroy(_instancedPhysicsMaterial);
            _instancedPhysicsMaterial = null;
        }

        if (_modifierParticles != null)
        {
            _modifierParticles.Stop();
            _modifierParticles.Clear();
        }

        if (_trailRenderer != null)
        {
            _trailRenderer.Clear();
        }

        _appliedModifiers.Clear();

        SoftReset();
    }

    public int GetAppliedModifierCount()
    {
        return _appliedModifiers.Count;
    }

    private void CheckValidModifierTarget(GenericModifierData selectedModifier)
    {
        _lastSelectedModifier = selectedModifier;
        _isCurrentlyValidTarget = CanAcceptModifier(selectedModifier);
        UpdateVisualState();
    }

    private void UpdateVisualState()
    {
        _isCurrentlyValidTarget = CanAcceptModifier(_lastSelectedModifier);

        if (_highlightRenderer != null)
        {
            _highlightRenderer.enabled = _isCurrentlyValidTarget;
        }
        if (_borderRenderer != null)
        {
            _borderRenderer.startColor = _borderColor;
            _borderRenderer.endColor = _borderColor;
            _borderRenderer.enabled = !_isCurrentlyValidTarget;
        }
        if (_appliedModifiers.Count > 0)
        {
            var appliedModifiersArray = _appliedModifiers.ToArray();
            // Use the latest applied modifier as the basis for visual data and colours
            GenericModifierData primaryData = appliedModifiersArray[0].ModifierData;
            Color primaryColor = primaryData.modifierColor;
            Color secondaryColor = primaryColor;

            if (appliedModifiersArray.Length > 1)
            {
                // If there are multiple, use the oldest (or second latest) for the sprite body
                secondaryColor = appliedModifiersArray[appliedModifiersArray.Length - 1].ModifierData.modifierColor;
            }

            // Apply secondary (the original applied by the player) colour to the sprites that form this object
            foreach (SpriteRenderer spriteRenderer in _childSpriteRenderers)
            {
                if (primaryData.modifierMaterial != null) spriteRenderer.material = primaryData.modifierMaterial;
                spriteRenderer.color = new Color(secondaryColor.r, secondaryColor.g, secondaryColor.b, spriteRenderer.color.a);
            }

            // Apply primary (last applied) colour to the object effects (particles, border and trail)
            Color effectColor = primaryColor * 0.9f;
            effectColor.a = 1;

            if (_modifierParticles != null)
            {
                ParticleSystem.MainModule mainModule = _modifierParticles.main;
                mainModule.startColor = effectColor;
                if (!_modifierParticles.isPlaying) _modifierParticles.Play();
            }

            if (_borderRenderer != null)
            {
                _borderRenderer.startColor = effectColor;
                _borderRenderer.endColor = effectColor;
            }

            if (_trailRenderer != null)
            {
                _trailRenderer.startColor = effectColor;
            }
        }
        else
        {
            // If modifier isn't enabled, we want to revert to the original visual state
            foreach (SpriteRenderer spriteRenderer in _childSpriteRenderers)
            {
                spriteRenderer.material = _defaultVisualMaterial;
                spriteRenderer.color = new Color(_defaultColor.r, _defaultColor.g, _defaultColor.b, spriteRenderer.color.a);
            }

            if (_modifierParticles != null && _modifierParticles.isPlaying) _modifierParticles.Stop();
            if (_trailRenderer != null) _trailRenderer.Clear();
        }
    }

    /// <summary>
    /// Dynamically generate a mesh of the CompositeCollider collider for this interactable object and set it as the
    /// volume from which particles are emitted
    /// </summary>
    private void ConfigureParticleShape()
    {
        if (_modifierParticles == null) return;

        CompositeCollider2D compositeCollider = _collider2d as CompositeCollider2D;
        if (compositeCollider == null) return;

        ParticleSystem.MainModule mainModule = _modifierParticles.main;
        mainModule.scalingMode = ParticleSystemScalingMode.Shape;
        ParticleSystem.ShapeModule shapeModule = _modifierParticles.shape;

        // Generate a mesh from the collider in local space
        Mesh geometryMesh = compositeCollider.CreateMesh(false, false);

        shapeModule.shapeType = ParticleSystemShapeType.Mesh;
        shapeModule.meshShapeType = ParticleSystemMeshShapeType.Triangle; // Triangle emits from the surface area of the shape
        shapeModule.mesh = geometryMesh;

        // Add padding to the emmitter (so it looks like particles are coming from slightly around the shape)
        float paddingScale = 1f + _particleBorderPadding;
        shapeModule.scale = new Vector3(paddingScale, paddingScale, 1f);

        // Centre the particle system at the origin as the new generated mesh inherently matches the parent object local space
        _modifierParticles.transform.localPosition = Vector3.zero;
    }

    /// <summary>
    /// Generates an outline of this interactable object based on it's compositeCollider collider component.
    /// The linerenderer returned must be enabled manually
    /// </summary>
    private LineRenderer OutlineGenerator(string name, Color outlineColor, float outlineThickness, int renderingOrder)
    {
        // Ball doesn't need to be highlighted.
        if (GetComponent<BallActor>() != null) return null;

        GameObject outlineObject = new GameObject(name);
        outlineObject.transform.SetParent(transform);
        outlineObject.transform.localPosition = Vector3.zero;
        outlineObject.transform.localRotation = Quaternion.identity;
        outlineObject.transform.localScale = Vector3.one;

        LineRenderer lineRenderer = outlineObject.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = false;
        lineRenderer.sortingOrder = renderingOrder;
        lineRenderer.loop = true;
        lineRenderer.startWidth = outlineThickness;
        lineRenderer.endWidth = outlineThickness;
        lineRenderer.startColor = outlineColor;
        lineRenderer.endColor = outlineColor;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

        CompositeCollider2D compositeCollider = GetComponent<CompositeCollider2D>();
        
        if (compositeCollider == null || compositeCollider.pathCount == 0)
        {
            Debug.LogWarning($"<color=yellow>{gameObject.name}: No CompositeCollider2D/paths found!</color>");
            return null;
        }

        // GetPath[0] for a composite collider returns the outline
        Vector2[] path = new Vector2[compositeCollider.GetPathPointCount(0)];
        compositeCollider.GetPath(0, path);

        // https://learn.microsoft.com/en-us/dotnet/api/system.array.convertall?view=net-10.0
        Vector3[] positions = System.Array.ConvertAll(path, new System.Converter<Vector2, Vector3>(Vector2ToVector3));

        lineRenderer.positionCount = positions.Length;
        lineRenderer.SetPositions(positions);

        lineRenderer.enabled = false;

        return lineRenderer;
    }

    private Vector3 Vector2ToVector3(Vector2 vector2)
    {
        Vector3 vector3 = vector2;
        return vector3;
    }
}