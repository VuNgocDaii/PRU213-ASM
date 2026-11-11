using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OrderUI : MonoBehaviour
{
    [Header("Controllers")]
    [SerializeField] private OrderController orderController;
    [SerializeField] private FeatureReferences featureReferences;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI orderInfoText;      // Hiện: Đơn hàng, số chai có/cần, tiền thưởng
    [SerializeField] private TextMeshProUGUI orderProgressText;  // Hiện: Tiến độ 1/5 đơn
    [SerializeField] private TextMeshProUGUI notificationText;   // Hiện thông báo lỗi màu đỏ
    [SerializeField] private Button deliverButton;               // Nút nhấn giao hàng
    [SerializeField] private GameObject demoCompletePanel;       // Panel hoàn thành demo 5 đơn

    private Coroutine notificationCoroutine;
    private InventoryManager subscribedInventory;

    private void OnEnable()
    {
        if (orderController != null)
        {
            orderController.OnOrderChanged += RefreshUI;
            orderController.OnDeliveryFailed += ShowNotification;
            orderController.OnAllOrdersCompleted += ShowCompleteScreen;
        }

        if (deliverButton != null)
        {
            deliverButton.onClick.AddListener(OnDeliverButtonClicked);
        }

        SubscribeInventory();

        if (demoCompletePanel != null)
        {
            demoCompletePanel.SetActive(orderController != null && orderController.IsCompletedAll);
        }

        RefreshUI();
    }

    private void Start()
    {
        // Scene initializers finish Awake before Start, but may run after this OnEnable.
        SubscribeInventory();
        RefreshUI();
    }

    private void SubscribeInventory()
    {
        InventoryManager inventory = featureReferences != null ? featureReferences.Inventory : null;
        if (inventory == subscribedInventory) return;
        if (subscribedInventory != null) subscribedInventory.OnChanged -= RefreshUI;
        subscribedInventory = inventory;
        if (subscribedInventory != null) subscribedInventory.OnChanged += RefreshUI;
    }

    private void OnDisable()
    {
        if (orderController != null)
        {
            orderController.OnOrderChanged -= RefreshUI;
            orderController.OnDeliveryFailed -= ShowNotification;
            orderController.OnAllOrdersCompleted -= ShowCompleteScreen;
        }

        if (deliverButton != null)
        {
            deliverButton.onClick.RemoveListener(OnDeliverButtonClicked);
        }

        if (subscribedInventory != null)
        {
            subscribedInventory.OnChanged -= RefreshUI;
            subscribedInventory = null;
        }
    }

    private void OnDeliverButtonClicked()
    {
        if (orderController != null)
        {
            orderController.TryDeliverOrder();
        }
    }

    public void RefreshUI()
    {
        if (orderController == null) return;

        // Cập nhật tiến độ đơn
        if (orderProgressText != null)
        {
            orderProgressText.text = orderController.HasOrderLimit
                ? $"Tiến độ: {orderController.CompletedOrdersCount}/{orderController.TargetOrderCount} đơn"
                : $"Đã giao: {orderController.CompletedOrdersCount} đơn";
        }

        // Cập nhật chi tiết đơn hiện tại
        if (orderController.IsCompletedAll)
        {
            if (orderInfoText != null)
            {
                orderInfoText.text = $"<b>ĐÃ HOÀN THÀNH TOÀN BỘ {orderController.TargetOrderCount} ĐƠN!</b>";
            }
            if (deliverButton != null)
            {
                deliverButton.interactable = false;
            }
            return;
        }

        OrderData order = orderController.CurrentOrder;
        if (order != null && orderInfoText != null)
        {
            int currentBottles = 0;
            if (featureReferences != null && featureReferences.Inventory != null)
            {
                currentBottles = featureReferences.Inventory.GetAmount(ItemType.BottledMilk);
            }

            string colorTag = currentBottles >= order.requiredBottledMilk ? "<color=#2ECC71>" : "<color=#E74C3C>";

            orderInfoText.text = $"<b>{order.orderName}</b>\n" +
                                 $"Sữa chai: {colorTag}{currentBottles}</color> / {order.requiredBottledMilk}\n" +
                                 $"Thưởng: <color=#F1C40F>+{order.rewardCredits} Credits</color>";
        }

        if (deliverButton != null)
        {
            deliverButton.interactable = !orderController.IsDelivering;
        }
    }

    public void ShowNotification(string message)
    {
        if (notificationText == null) return;

        if (notificationCoroutine != null)
        {
            StopCoroutine(notificationCoroutine);
        }
        notificationCoroutine = StartCoroutine(NotificationRoutine(message));
    }

    private IEnumerator NotificationRoutine(string message)
    {
        notificationText.text = message;
        notificationText.gameObject.SetActive(true);
        yield return new WaitForSeconds(3.0f);
        notificationText.gameObject.SetActive(false);
        notificationCoroutine = null;
    }

    private void ShowCompleteScreen()
    {
        if (demoCompletePanel != null)
        {
            demoCompletePanel.SetActive(true);
        }
    }
}
