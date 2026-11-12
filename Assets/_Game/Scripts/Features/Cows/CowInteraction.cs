using UnityEngine;

[DisallowMultipleComponent]
public class CowInteraction : Interactable
{
    [Header("Tham chiếu")]
    [SerializeField] private CowController cowController;
    [SerializeField] private CowHealth cowHealth;
    [SerializeField] private FeatureReferences featureReferences;

    public CowHealth Health => cowHealth != null ? cowHealth : (cowHealth = GetComponent<CowHealth>());

    private PlayerInteraction cachedPlayer;

    private void Awake()
    {
        if (cowController == null) cowController = GetComponent<CowController>();
        if (cowHealth == null) cowHealth = GetComponent<CowHealth>();
        if (featureReferences == null) featureReferences = GetComponentInParent<FeatureReferences>();
        UpdateHintText();
    }

    private void Update()
    {
        UpdateHintText();
        CheckSecondaryInput();
    }

    private bool IsPlayerInRange()
    {
        if (cachedPlayer == null) cachedPlayer = FindAnyObjectByType<PlayerInteraction>();
        if (cachedPlayer == null) return false;

        float maxRange = 2.0f;
        return ((Vector2)cachedPlayer.transform.position - (Vector2)transform.position).sqrMagnitude <= maxRange * maxRange;
    }

    private void CheckSecondaryInput()
    {
        if (!IsPlayerInRange()) return;

        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null && (keyboard.fKey.wasPressedThisFrame || keyboard.hKey.wasPressedThisFrame))
        {
            TryHealCow();
        }
    }

    public bool TryHealCow()
    {
        var health = Health;
        if (health == null || health.IsHealthy) return false;

        var inventory = GetInventory();
        if (inventory == null || inventory.GetAmount(ItemType.Medicine) <= 0)
        {
            Debug.LogWarning($"[CowInteraction] {name}: Không có Medicine để chữa bệnh [{health.CurrentDisease}].", this);
            return false;
        }

        var before = health.CurrentDisease;
        if (health.TryHeal(inventory))
        {
            Debug.Log($"[CowInteraction] {name}: [F] Dùng 1 Medicine chữa bớt bệnh ({before} -> {health.CurrentDisease})!", this);
            UpdateHintText();
            return true;
        }

        return false;
    }

    public override void Interact()
    {
        if (cowController == null) cowController = GetComponent<CowController>();
        if (cowController == null) return;

        switch (cowController.CurrentState)
        {
            case CowState.Idle:
                cowController.TryFeed();
                break;
            case CowState.Producing:
                Debug.Log($"[CowInteraction] Bò đang tạo sữa. Còn lại: {cowController.RemainingTime:F1}s.", this);
                break;
            case CowState.Ready:
                cowController.TryCollectMilk();
                break;
        }

        UpdateHintText();
    }

    public new string GetInteractionText() => BuildCurrentText();

    private void UpdateHintText()
    {
        var field = typeof(Interactable).GetField("interactionText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(this, BuildCurrentText());
    }

    private string BuildCurrentText()
    {
        if (cowController == null) cowController = GetComponent<CowController>();
        if (cowController == null) return "Kiểm tra bò";

        string mainAction = cowController.CurrentState switch
        {
            CowState.Idle => $"Cho bò ăn ({cowController.RequiredFeedCost} Feed)",
            CowState.Producing => $"Đang tạo sữa ({cowController.RemainingTime:F0}s)",
            CowState.Ready => $"Thu hoạch sữa ({cowController.LockedMilkAmount} SP)",
            _ => "Tương tác"
        };

        var health = Health;
        if (health != null && !health.IsHealthy)
        {
            int medicineNeeded = health.CurrentDisease == DiseaseLevel.Severe ? 2 : 1;
            var inv = GetInventory();
            bool hasMedicine = inv != null && inv.GetAmount(ItemType.Medicine) >= medicineNeeded;
            string diseaseTag = hasMedicine
                ? $" | [F: Chữa bệnh {health.CurrentDisease} (-1 Thuốc)]"
                : $" | [Bò bệnh {health.CurrentDisease} - Cần {medicineNeeded} Thuốc]";
            return mainAction + diseaseTag;
        }

        return mainAction;
    }

    private InventoryManager GetInventory()
    {
        if (featureReferences != null && featureReferences.Inventory != null)
        {
            return featureReferences.Inventory;
        }

        return null;
    }

    [ContextMenu("Test/Press E (Feed or Collect)")]
    public void TestInteract() => Interact();

    [ContextMenu("Test/Press F (Heal with 1 Medicine)")]
    public void TestHealWithF() => TryHealCow();
}
