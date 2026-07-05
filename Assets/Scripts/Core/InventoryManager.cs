using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public record ModifierAllocation
{
    public GenericModifierData modifier;
    public int startingCount;
}

/// <summary>
/// Manager to handle inventory state changes and modifier usage
/// </summary>
// DefaultExecutionOrder makes sure this script Awake/Start method run before the other scripts (which default to 0).
// Need to do this for singleton managers as other scripts rely on them during initialisation.
[DefaultExecutionOrder(-10)]
public class InventoryManager : MonoBehaviour
{
    [Header("Level Inventory Configuration")]
    [SerializeField] private List<ModifierAllocation> _initialAllocations = new List<ModifierAllocation>();

    public static InventoryManager Instance { get; private set; }

    /// <summary>
    /// Event fired when inventory modifier counts change - contains the modifier that changed and it's new quantity
    /// </summary>
    public event Action<GenericModifierData, int> OnInventoryChanged;
    private Dictionary<GenericModifierData, int> _currentInventory = new Dictionary<GenericModifierData, int>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
            return;
        }

        InitialiseInventory();
    }

    /// <summary>
    /// Retrieves the current inventory quantity for a specific modifier
    /// </summary>
    public int GetCount(GenericModifierData modifier)
    {
        return _currentInventory.TryGetValue(modifier, out int count) ? count : 0;
    }

    /// <summary>
    /// Checks if a modifier has atleast a quantity of one
    /// </summary>
    public bool HasModifier(GenericModifierData modifier)
    {
        return GetCount(modifier) > 0;
    }

    /// <summary>
    /// Attempts to consume one of the specified modifier from the inventory
    /// </summary>
    /// <returns>True if successfull, false if there were none available</returns>
    public bool ConsumeModifier(GenericModifierData modifier)
    {
        if (HasModifier(modifier))
        {
            _currentInventory[modifier]--;
            OnInventoryChanged?.Invoke(modifier, _currentInventory[modifier]);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Adds one of the specified modifier back into the inventory
    /// </summary>
    public void RefundModifier(GenericModifierData modifier)
    {
        if (_currentInventory.ContainsKey(modifier))
        {
            _currentInventory[modifier]++;
            OnInventoryChanged?.Invoke(modifier, _currentInventory[modifier]);
        }
    }

    private void InitialiseInventory()
    {
        _currentInventory.Clear();

        foreach (ModifierAllocation allocation in _initialAllocations)
        {
            if (allocation.modifier != null)
            {
                _currentInventory[allocation.modifier] = allocation.startingCount;
            }
        }
    }

    /// <summary>
    /// Debug method to get all the modifier types currently stored
    /// 
    /// Credit to Kirk Woll for the LINQ code
    /// Source - https://stackoverflow.com/a/10255134
    /// Retrieved 2026-04-09, License - CC BY-SA 3.0
    /// 
    /// </summary>
    public IEnumerable<GenericModifierData> GetAllModifierTypes()
    {
        return _initialAllocations.Select(x => x.modifier).Distinct();
    }
}