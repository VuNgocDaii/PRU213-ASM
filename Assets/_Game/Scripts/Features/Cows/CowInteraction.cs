using System.Reflection;
using UnityEngine;

[DisallowMultipleComponent]
public class CowInteraction : Interactable
{
    [Header("Tham chiếu")]
    [SerializeField] private CowController cowController;
    [SerializeField] private CowHealth cowHealth;
    [SerializeField] private FeatureReferences featureReferences;

    public CowHealth Health => cowHealth;

    private static readonly FieldInfo InteractionTextField =
        typeof(Interactable).GetField("interactionText", BindingFlags.NonPublic | BindingFlags.Instance);

    private void Awake()
    {
        UpdateHintText();
    }

    private PlayerInteraction cachedPlayer;

    private void Update()
    {
        UpdateHintText();
        CheckSecondaryInput();
    }

    private bool IsPlayerInRange()
    {
        if (cachedPlayer == null)
        {
            cachedPlayer = FindAnyObjectByType<PlayerInteraction>();
        }
        if (cachedPlayer == null) return false;

        float maxRange = 2.0f;
        float distSq = ((Vector2)cachedPlayer.transform.position - (Vector2)transform.position).sqrMagnitude;
        return distSq <= maxRange * maxRange;
    }

    private void CheckSecondaryInput()
    {
        if (!IsPlayerInRange()) return;

        UnityEngine.InputSystem.Keyboard keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.fKey.wasPressedThisFrame || keyboard.hKey.wasPressedThisFrame)
        {
            TryHealCow();
        }
    }

    public bool TryHealCow()
    {
        var health = Health;
        if (health == null)
        {
            Debug.LogWarning("[CowInteraction] Bò không có CowHealth.", this);
            return false;
        }

        if (health.IsHealthy)
        {
            Debug.Log("[CowInteraction] Bò đang hoàn toàn khỏe mạnh, không cần chữa.", this);
            return false;
        }

        InventoryManager inventory = InventoryManager.Instance;
        if (inventory == null || inventory.GetAmount(ItemType.Medicine) <= 0)
        {
            Debug.LogWarning($"[CowInteraction] {name}: Không có Medicine để chữa bệnh [{health.CurrentDisease}]. Hãy đến MedicineShop mua thuốc trước (20 Credits).", this);
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
        if (cowController == null)
        {
            cowController = GetComponent<CowController>();
        }
        if (cowController == null)
        {
            Debug.LogWarning("[CowInteraction] Chưa gán CowController.", this);
            return;
        }

        switch (cowController.CurrentState)
        {
            case CowState.Idle:
                cowController.TryFeed();
                break;

            case CowState.Producing:
                Debug.Log($"[CowInteraction] Bò đang nhai cỏ và tạo sữa. Còn lại: {cowController.RemainingTime:F1}s.", this);
                break;

            case CowState.Ready:
                cowController.TryCollectMilk();
                break;
        }

        UpdateHintText();
    }

    public new string GetInteractionText()
    {
        return BuildCurrentText();
    }

    private void UpdateHintText()
    {
        if (InteractionTextField != null)
        {
            InteractionTextField.SetValue(this, BuildCurrentText());
        }
    }

    private string BuildCurrentText()
    {
        if (cowController == null)
        {
            cowController = GetComponent<CowController>();
        }
        if (cowController == null)
        {
            return "Kiểm tra bò";
        }

        var health = Health;
        InventoryManager inventory = InventoryManager.Instance;

        string mainAction = "";
        switch (cowController.CurrentState)
        {
            case CowState.Idle:
                mainAction = $"Cho bò ăn ({cowController.RequiredFeedCost} Feed)";
                break;

            case CowState.Producing:
                mainAction = $"Đang tạo sữa ({cowController.RemainingTime:F0}s)";
                break;

            case CowState.Ready:
                mainAction = $"Thu hoạch sữa ({cowController.LockedMilkAmount} SP)";
                break;

            default:
                mainAction = "Tương tác";
                break;
        }

        if (health != null && !health.IsHealthy)
        {
            int medicineNeeded = health.CurrentDisease == DiseaseLevel.Severe ? 2 : 1;
            bool hasMedicine = inventory != null && inventory.GetAmount(ItemType.Medicine) >= medicineNeeded;
            string diseaseTag = hasMedicine
                ? $" | [F: Chữa bệnh {health.CurrentDisease} (-1 Thuốc)]"
                : $" | [Bò bệnh {health.CurrentDisease} - Cần {medicineNeeded} Thuốc]";
            return mainAction + diseaseTag;
        }

        return mainAction;
    }

    [ContextMenu("Test/Press E (Feed or Collect)")]
    public void TestInteract()
    {
        Interact();
    }

    [ContextMenu("Test/Press F (Heal with 1 Medicine)")]
    public void TestHealWithF()
    {
        TryHealCow();
    }
}
