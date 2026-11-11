using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "PolishCow/Building Catalog", fileName = "BuildingCatalog")]
public class BuildingCatalogData : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string id;
        public string displayName;
        public DefenseBuildingSlot.Kind slotKind;
        public GameObject prefab;
        [Min(1)] public int buildPrice = 100;
        [Range(1, 3)] public int unlockCastleLevel = 1;
        public Vector2 footprintSize = new Vector2(1.2f, 1.2f);
        public int[] upgradePrices = new int[0];
        [Range(0.1f, 1f)] public float refundRatio = 0.5f;
        public Sprite icon;
        [TextArea(2, 4)] public string description;

        public bool CanBuild(CastleProgress progress, InventoryManager inventory, DefenseBuildingSlot slot, out string reason)
        {
            reason = null;
            if (slot == null)
            {
                reason = "Không tìm thấy ô xây";
                return false;
            }
            if (!slot.IsUnlocked)
            {
                reason = "Ô chưa mở khóa trên bản đồ";
                return false;
            }
            if (slot.Occupant != null)
            {
                reason = "Vị trí đã có công trình";
                return false;
            }
            if (slot.SlotKind != slotKind)
            {
                reason = $"Sai loại ô (yêu cầu ô {slotKind})";
                return false;
            }
            if (progress != null && progress.CurrentLevel < unlockCastleLevel)
            {
                reason = $"Chưa mở khóa (Yêu cầu thành cấp {unlockCastleLevel})";
                return false;
            }
            if (inventory == null || inventory.GetAmount(ItemType.Credits) < buildPrice)
            {
                reason = $"Không đủ Credits (cần {buildPrice})";
                return false;
            }
            if (!BuildingFootprintUtility.IsFootprintClear(slot.transform.position, footprintSize, slot.transform))
            {
                reason = "Vị trí có vật cản";
                return false;
            }
            return true;
        }
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();
    public IReadOnlyList<Entry> Entries => entries;

    public Entry FindById(string id)
    {
        if (string.IsNullOrEmpty(id) || entries == null) return null;
        return entries.Find(e => string.Equals(e.id, id, StringComparison.OrdinalIgnoreCase));
    }

    public List<Entry> GetEntriesForKind(DefenseBuildingSlot.Kind kind, int maxCastleLevel = 3)
    {
        var result = new List<Entry>();
        if (entries == null) return result;
        foreach (var entry in entries)
        {
            if (entry != null && entry.slotKind == kind && entry.unlockCastleLevel <= maxCastleLevel)
                result.Add(entry);
        }
        return result;
    }
}
