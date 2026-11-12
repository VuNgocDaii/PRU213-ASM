using UnityEngine;

public enum CowState
{
    Idle,
    Producing,
    Ready
}

[DisallowMultipleComponent]
public class CowController : MonoBehaviour
{
    [Header("Tham chiếu")]
    [SerializeField] private FeatureReferences featureReferences;
    [SerializeField] private CowUpgrade cowUpgrade;
    [SerializeField] private CowHealth cowHealth;

    [Header("Trạng thái hiện tại")]
    [SerializeField] private CowState currentState = CowState.Idle;
    [SerializeField] private float remainingTime;
    [SerializeField] private float lockedDuration;
    [SerializeField] private int lockedMilkAmount = 1;

    public CowState CurrentState => currentState;
    public float RemainingTime => remainingTime;
    public float LockedDuration => lockedDuration;
    public int LockedMilkAmount => lockedMilkAmount;

    public int RequiredFeedCost => cowUpgrade != null ? cowUpgrade.FeedCost : 1;
    public int ExpectedMilkAmount => cowUpgrade != null ? cowUpgrade.MilkYield : 1;

    public void Initialize(FeatureReferences references, CowUpgrade upgrade = null, CowHealth health = null)
    {
        featureReferences = references;
        if (upgrade != null) cowUpgrade = upgrade;
        if (health != null) cowHealth = health;
    }

    private void Awake()
    {
        if (featureReferences == null) featureReferences = GetComponentInParent<FeatureReferences>();
        if (cowUpgrade == null) cowUpgrade = GetComponentInParent<CowUpgrade>();
        if (cowHealth == null) cowHealth = GetComponent<CowHealth>();
    }

    private void Update()
    {
        if (currentState != CowState.Producing) return;

        remainingTime -= Time.deltaTime;
        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            currentState = CowState.Ready;
            Debug.Log($"[CowController] Bò đã tạo xong sữa! Sẵn sàng thu hoạch ({lockedMilkAmount} sản phẩm).", this);
        }
    }

    public bool TryFeed()
    {
        if (featureReferences == null || featureReferences.Inventory == null)
        {
            Debug.LogWarning("[CowController] Chưa được Initialize FeatureReferences với kho hợp lệ.", this);
            return false;
        }

        if (currentState != CowState.Idle) return false;

        InventoryManager inventory = featureReferences.Inventory;
        int feedCost = RequiredFeedCost;
        int milkAmount = ExpectedMilkAmount;
        float baseDuration = cowUpgrade != null ? cowUpgrade.ProductionDuration : 10f;
        float diseaseMult = cowHealth != null ? cowHealth.DurationMultiplier : 1f;
        float duration = baseDuration * diseaseMult;

        if (milkAmount <= 0 || inventory.GetAmount(ItemType.RawMilk) > int.MaxValue - milkAmount || inventory.GetAmount(ItemType.SpoiledMilk) > int.MaxValue - milkAmount)
        {
            Debug.LogWarning("[CowController] Kho sữa đã đầy hoặc số lượng không hợp lệ.", this);
            return false;
        }

        if (!inventory.TryRemove(ItemType.Feed, feedCost))
        {
            Debug.Log("[CowController] Không đủ Feed để cho bò ăn.", this);
            return false;
        }

        lockedDuration = duration;
        remainingTime = duration;
        lockedMilkAmount = milkAmount;
        currentState = CowState.Producing;

        Debug.Log($"[CowController] Cho bò ăn thành công (-{feedCost} Feed). Đang tạo sữa trong {lockedDuration:F1}s.", this);
        return true;
    }

    public bool TryCollectMilk()
    {
        if (featureReferences == null || featureReferences.Inventory == null)
        {
            Debug.LogWarning("[CowController] Chưa được Initialize FeatureReferences với kho hợp lệ.", this);
            return false;
        }

        if (currentState != CowState.Ready) return false;

        InventoryManager inventory = featureReferences.Inventory;

        if (lockedMilkAmount <= 0 || inventory.GetAmount(ItemType.RawMilk) > int.MaxValue - lockedMilkAmount || inventory.GetAmount(ItemType.SpoiledMilk) > int.MaxValue - lockedMilkAmount)
        {
            Debug.LogWarning("[CowController] Không thể cộng sữa vì vượt giới hạn lưu trữ.", this);
            return false;
        }

        AddMilkByDisease(inventory, lockedMilkAmount);

        currentState = CowState.Idle;
        remainingTime = 0f;
        lockedDuration = 0f;
        return true;
    }

    private void AddMilkByDisease(InventoryManager inventory, int milkAmount)
    {
        DiseaseLevel disease = cowHealth != null ? cowHealth.CurrentDisease : DiseaseLevel.Healthy;

        switch (disease)
        {
            case DiseaseLevel.Severe:
                inventory.Add(ItemType.SpoiledMilk, milkAmount);
                Debug.Log($"[CowController] Bò bệnh Severe → +{milkAmount} SpoiledMilk.", this);
                break;

            case DiseaseLevel.Moderate:
                int earned = cowHealth != null ? cowHealth.AccumulateMilk(milkAmount * 0.5f) : milkAmount;
                if (earned > 0)
                {
                    inventory.Add(ItemType.RawMilk, earned);
                    Debug.Log($"[CowController] Bò bệnh Moderate: tích lũy đủ → +{earned} RawMilk.", this);
                }
                break;

            default:
                inventory.Add(ItemType.RawMilk, milkAmount);
                Debug.Log($"[CowController] Thu hoạch sữa thành công (+{milkAmount} RawMilk).", this);
                break;
        }
    }
}
