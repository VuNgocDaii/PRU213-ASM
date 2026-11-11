using System;
using UnityEngine;

public class OrderController : MonoBehaviour
{
    [Header("Base References")]
    [SerializeField] private FeatureReferences featureReferences;
    [SerializeField] private DroneDeliveryEffect droneEffect;

    [Header("Order Configuration")]
    [SerializeField, Min(1)] private int minBottles = 2;
    [SerializeField, Min(1)] private int maxBottles = 5;
    [SerializeField, Min(1)] private int creditsPerBottle = 25;
    [SerializeField, Min(1)] private int targetOrderCount = 5;
    [SerializeField] private bool limitDemoOrders = false;

    public OrderData CurrentOrder { get; private set; }
    public int CompletedOrdersCount { get; private set; } = 0;
    public int TargetOrderCount => targetOrderCount;
    public bool HasOrderLimit => limitDemoOrders;
    public bool IsCompletedAll => limitDemoOrders && CompletedOrdersCount >= targetOrderCount;
    public bool IsDelivering { get; private set; } = false;

    // Events để UI lắng nghe cập nhật
    public event Action OnOrderChanged;
    public event Action<string> OnDeliveryFailed;
    public event Action OnAllOrdersCompleted;

    private void Start()
    {
        GenerateNextOrder();
    }

    public void GenerateNextOrder()
    {
        if (IsCompletedAll) return;

        int bottles = UnityEngine.Random.Range(minBottles, maxBottles + 1);
        int reward = bottles * creditsPerBottle;
        string orderName = $"Đơn hàng #{CompletedOrdersCount + 1}";

        CurrentOrder = new OrderData(orderName, bottles, reward);
        OnOrderChanged?.Invoke();
    }

    public bool TryDeliverOrder()
    {
        // 1. Kiểm tra trạng thái hợp lệ
        if (IsDelivering)
        {
            return false;
        }

        if (IsCompletedAll)
        {
            OnDeliveryFailed?.Invoke($"Bạn đã hoàn thành toàn bộ ({targetOrderCount}/{targetOrderCount} đơn)!");
            return false;
        }

        if (featureReferences == null || featureReferences.Inventory == null)
        {
            Debug.LogError("OrderController: FeatureReferences hoặc Inventory chưa được Initialize!");
            return false;
        }

        InventoryManager inventory = featureReferences.Inventory;

        if (CurrentOrder == null)
        {
            GenerateNextOrder();
            return false;
        }

        // 2. Kiểm tra đủ BottledMilk không
        int currentBottles = inventory.GetAmount(ItemType.BottledMilk);
        if (currentBottles < CurrentOrder.requiredBottledMilk)
        {
            OnDeliveryFailed?.Invoke($"Thiếu hàng! Cần {CurrentOrder.requiredBottledMilk} chai (Hiện có: {currentBottles}).");
            return false;
        }

        // 3. Kiểm tra tràn số int Credits trước khi nhận
        if (inventory.GetAmount(ItemType.Credits) > int.MaxValue - CurrentOrder.rewardCredits)
        {
            OnDeliveryFailed?.Invoke("Túi tiền đã đầy!");
            return false;
        }

        // 4. Khóa giao dịch và trừ kho
        IsDelivering = true;
        bool removed = inventory.TryRemove(ItemType.BottledMilk, CurrentOrder.requiredBottledMilk);
        if (!removed)
        {
            IsDelivering = false;
            OnDeliveryFailed?.Invoke("Giao dịch thất bại!");
            return false;
        }

        // 5. Cộng tiền thưởng
        inventory.Add(ItemType.Credits, CurrentOrder.rewardCredits);

        // 6. Kích hoạt hiệu ứng Drone
        if (droneEffect != null)
        {
            droneEffect.PlayDelivery(() =>
            {
                FinishDelivery();
            });
        }
        else
        {
            FinishDelivery();
        }

        return true;
    }

    private void FinishDelivery()
    {
        CompletedOrdersCount++;
        IsDelivering = false;

        if (IsCompletedAll)
        {
            OnAllOrdersCompleted?.Invoke();
            OnOrderChanged?.Invoke();
        }
        else
        {
            GenerateNextOrder();
        }
    }
}
