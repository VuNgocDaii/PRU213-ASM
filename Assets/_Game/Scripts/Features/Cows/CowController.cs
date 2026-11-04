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

    [Header("Trạng thái hiện tại")]
    [SerializeField] private CowState currentState = CowState.Idle;
    [SerializeField] private float remainingTime;
    [SerializeField] private float lockedDuration;
    [SerializeField] private int lockedYield = 1;

    public CowState CurrentState => currentState;
    public float RemainingTime => remainingTime;
    public float LockedDuration => lockedDuration;
    public int LockedYield => lockedYield;

    public int RequiredFeedCost => cowUpgrade != null ? cowUpgrade.FeedCost : 1;
    public int PreviewYield => cowUpgrade != null ? cowUpgrade.MilkYield : 1;

    public void Initialize(FeatureReferences references, CowUpgrade upgrade = null)
    {
        featureReferences = references;
        if (upgrade != null)
        {
            cowUpgrade = upgrade;
        }
    }

    private void Awake()
    {
        if (featureReferences == null)
        {
            featureReferences = GetComponentInParent<FeatureReferences>();
        }
        if (cowUpgrade == null)
        {
            cowUpgrade = GetComponentInParent<CowUpgrade>();
        }
    }

    private void Update()
    {
        if (currentState != CowState.Producing)
        {
            return;
        }

        remainingTime -= Time.deltaTime;
        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            currentState = CowState.Ready;
            Debug.Log($"[CowController] Bò đã tạo xong sữa! Sẵn sàng thu hoạch ({lockedYield} RawMilk).", this);
        }
    }

    public bool TryFeed()
    {
        if (featureReferences == null || featureReferences.Inventory == null)
        {
            Debug.LogWarning("[CowController] Chưa được Initialize với kho hợp lệ.", this);
            return false;
        }

        if (currentState != CowState.Idle)
        {
            return false;
        }

        InventoryManager inventory = featureReferences.Inventory;
        int feedCost = RequiredFeedCost;
        int yield = PreviewYield;
        float duration = cowUpgrade != null ? cowUpgrade.ProductionDuration : 10f;

        // Kiểm tra phòng ngừa tràn số trước khi thực hiện giao dịch
        if (yield <= 0 || inventory.GetAmount(ItemType.RawMilk) > int.MaxValue - yield)
        {
            Debug.LogWarning("[CowController] Kho RawMilk đã đầy hoặc số lượng sản phẩm không hợp lệ.", this);
            return false;
        }

        if (!inventory.TryRemove(ItemType.Feed, feedCost))
        {
            Debug.Log("[CowController] Không đủ Feed để cho bò ăn.", this);
            return false;
        }

        // Chốt thông số cho lượt nuôi hiện tại (không bị ảnh hưởng nếu nâng cấp giữa chừng)
        lockedDuration = duration;
        remainingTime = duration;
        lockedYield = yield;
        currentState = CowState.Producing;

        Debug.Log($"[CowController] Cho bò ăn thành công (-{feedCost} Feed). Đang tạo sữa trong {lockedDuration:F1}s.", this);
        return true;
    }

    public bool TryCollectMilk()
    {
        if (featureReferences == null || featureReferences.Inventory == null)
        {
            Debug.LogWarning("[CowController] Chưa được Initialize với kho hợp lệ.", this);
            return false;
        }

        if (currentState != CowState.Ready)
        {
            return false;
        }

        InventoryManager inventory = featureReferences.Inventory;

        if (lockedYield <= 0 || inventory.GetAmount(ItemType.RawMilk) > int.MaxValue - lockedYield)
        {
            Debug.LogWarning("[CowController] Không thể cộng RawMilk vì vượt giới hạn lưu trữ.", this);
            return false;
        }

        inventory.Add(ItemType.RawMilk, lockedYield);
        Debug.Log($"[CowController] Thu hoạch sữa thành công (+{lockedYield} RawMilk).", this);

        // Đặt lại trạng thái về Idle cho lượt nuôi mới
        currentState = CowState.Idle;
        remainingTime = 0f;
        lockedDuration = 0f;
        return true;
    }
}
