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
    [SerializeField] private int lockedYield = 1;

    public CowState CurrentState => currentState;
    public float RemainingTime => remainingTime;
    public float LockedDuration => lockedDuration;
    public int LockedYield => lockedYield;

    public int RequiredFeedCost => cowUpgrade != null ? cowUpgrade.FeedCost : 1;
    public int PreviewYield => cowUpgrade != null ? cowUpgrade.MilkYield : 1;

    public void Initialize(FeatureReferences references, CowUpgrade upgrade = null, CowHealth health = null)
    {
        featureReferences = references;
        if (upgrade != null)
        {
            cowUpgrade = upgrade;
        }
        if (health != null)
        {
            cowHealth = health;
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
        if (cowHealth == null)
        {
            cowHealth = GetComponent<CowHealth>();
            if (cowHealth == null)
            {
                cowHealth = gameObject.AddComponent<CowHealth>();
            }
        }

        var healthUI = GetComponent<CowHealthUI>();
        if (healthUI == null)
        {
            healthUI = gameObject.AddComponent<CowHealthUI>();
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
            Debug.Log($"[CowController] Bò đã tạo xong sữa! Sẵn sàng thu hoạch ({lockedYield} sản phẩm).", this);
        }
    }

    public InventoryManager GetInventory()
    {
        if (featureReferences != null && featureReferences.Inventory != null)
        {
            return featureReferences.Inventory;
        }

        InventoryManager inv = null;
        var module = GetComponentInParent<EconomyFeature>();
        if (module != null && module.Context != null && module.Context.Inventory != null)
        {
            inv = module.Context.Inventory;
        }

        if (inv == null)
        {
            var ctx = FindAnyObjectByType<DefenseSceneContext>();
            if (ctx != null && ctx.Inventory != null)
            {
                inv = ctx.Inventory;
            }
        }

        if (inv == null)
        {
            inv = FindAnyObjectByType<InventoryManager>();
        }

        if (inv != null && featureReferences != null && featureReferences.Inventory == null)
        {
            featureReferences.Initialize(inv);
        }

        return inv;
    }

    public bool TryFeed()
    {
        InventoryManager inventory = GetInventory();
        if (inventory == null)
        {
            Debug.LogWarning("[CowController] Chưa kết nối được với Inventory.", this);
            return false;
        }

        if (currentState != CowState.Idle)
        {
            return false;
        }

        int feedCost = RequiredFeedCost;
        int yield = PreviewYield;

        float baseDuration = cowUpgrade != null ? cowUpgrade.ProductionDuration : 10f;
        float diseaseMult = cowHealth != null ? cowHealth.DurationMultiplier : 1f;
        float duration = baseDuration * diseaseMult;

        if (yield <= 0 || inventory.GetAmount(ItemType.RawMilk) > int.MaxValue - yield || inventory.GetAmount(ItemType.SpoiledMilk) > int.MaxValue - yield)
        {
            Debug.LogWarning("[CowController] Kho sữa đã đầy hoặc số lượng sản phẩm không hợp lệ.", this);
            return false;
        }

        if (!inventory.TryRemove(ItemType.Feed, feedCost))
        {
            Debug.Log("[CowController] Không đủ Feed để cho bò ăn.", this);
            return false;
        }

        lockedDuration = duration;
        remainingTime = duration;
        lockedYield = yield;
        currentState = CowState.Producing;

        Debug.Log($"[CowController] Cho bò ăn thành công (-{feedCost} Feed). Đang tạo sữa trong {lockedDuration:F1}s.", this);
        return true;
    }

    public bool TryCollectMilk()
    {
        InventoryManager inventory = GetInventory();
        if (inventory == null)
        {
            Debug.LogWarning("[CowController] Chưa kết nối được với Inventory.", this);
            return false;
        }

        if (currentState != CowState.Ready)
        {
            return false;
        }

        if (lockedYield <= 0 || inventory.GetAmount(ItemType.RawMilk) > int.MaxValue - lockedYield || inventory.GetAmount(ItemType.SpoiledMilk) > int.MaxValue - lockedYield)
        {
            Debug.LogWarning("[CowController] Không thể cộng sữa vì vượt giới hạn lưu trữ.", this);
            return false;
        }

        AddMilkByDisease(inventory, lockedYield);

        currentState = CowState.Idle;
        remainingTime = 0f;
        lockedDuration = 0f;
        return true;
    }

    private void AddMilkByDisease(InventoryManager inventory, int yield)
    {
        var health = cowHealth;
        DiseaseLevel disease = health != null ? health.CurrentDisease : DiseaseLevel.Healthy;

        switch (disease)
        {
            case DiseaseLevel.Severe:
                inventory.Add(ItemType.SpoiledMilk, yield);
                Debug.Log($"[CowController] Bò bệnh Severe → +{yield} SpoiledMilk.", this);
                break;

            case DiseaseLevel.Moderate:
                int earned = health != null ? health.AccumulateMilk(yield * 0.5f) : yield;
                if (earned > 0)
                {
                    inventory.Add(ItemType.RawMilk, earned);
                    Debug.Log($"[CowController] Bò bệnh Moderate: tích lũy đủ → +{earned} RawMilk.", this);
                }
                else
                {
                    Debug.Log($"[CowController] Bò bệnh Moderate: tích lũy {health?.MilkAccumulator ?? 0f:F1}/1.0 chưa đủ.", this);
                }
                break;

            default:
                inventory.Add(ItemType.RawMilk, yield);
                Debug.Log($"[CowController] Thu hoạch sữa thành công (+{yield} RawMilk).", this);
                break;
        }
    }
}
