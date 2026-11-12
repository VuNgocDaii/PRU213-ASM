using System;
using UnityEngine;

// Shared placement contract. UI owns the selected prefab/price; this slot owns payment and occupancy.
public class DefenseBuildingSlot : MonoBehaviour
{
    public enum Kind { Tower, Cow, Machine }
    [SerializeField] private Kind kind;
    [SerializeField] private FeatureReferences references;
    [SerializeField] private CastleProgress progress;
    [SerializeField, Range(1, 3)] private int requiredLevel = 1;
    [SerializeField] private GameObject initialOccupant;
    [SerializeField] private DefenseSceneContext defenseContext;
    public Kind SlotKind => kind;
    public GameObject Occupant { get; private set; }
    public bool IsUnlocked => progress != null && progress.CurrentLevel >= requiredLevel;
    public event Action<GameObject> OnBuilt;
    private bool isBuilding;
    private void Awake() { Occupant = initialOccupant; }

    public bool ReleaseOccupant(GameObject expected)
    {
        if (expected == null || Occupant != expected || isBuilding) return false;
        Occupant = null;
        return true;
    }

    public bool TryBuild(GameObject prefab, int price, Kind requestedKind, int unlockCastleLevel = 1)
    {
        if (!IsUnlocked || requestedKind != kind || progress.CurrentLevel < unlockCastleLevel || isBuilding || Occupant != null || price <= 0 ||
            references == null || references.Inventory == null || prefab == null ||
            prefab.GetComponent<FeatureReferences>() == null || prefab.GetComponents<UpgradeTarget>().Length != 1) return false;
        InventoryManager inventory = references.Inventory;
        isBuilding = true;
        try
        {
            if (!inventory.TryRemove(ItemType.Credits, price)) return false;
            try
            {
                Occupant = Instantiate(prefab, transform.position, Quaternion.identity, transform);
                Occupant.GetComponent<FeatureReferences>().Initialize(inventory);
                foreach (FeatureReferences feature in Occupant.GetComponentsInChildren<FeatureReferences>(true)) feature.Initialize(inventory);
                if (defenseContext != null)
                    foreach (DefenseModule module in Occupant.GetComponentsInChildren<DefenseModule>(true)) module.Initialize(defenseContext);
            }
            catch (Exception exception)
            {
                if (Occupant != null) Destroy(Occupant);
                Occupant = null;
                inventory.Add(ItemType.Credits, price);
                Debug.LogException(exception, this);
                return false;
            }
        }
        finally { isBuilding = false; }
        OnBuilt?.Invoke(Occupant);
        return true;
    }
}
