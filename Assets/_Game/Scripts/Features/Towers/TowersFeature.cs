using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem; // Dùng New Input System

// Người 5: Quản lý toàn bộ tháp, đạn và UI chiến đấu
public class TowersFeature : DefenseModule
{
    [Header("UI References")]
    [SerializeField] private DefenseShopUI shopUI;
    [SerializeField] private TowerActionMenuUI actionMenuUI;
    [SerializeField] private BattleUI battleUI;

    protected override void OnConnected()
    {
        if (shopUI != null) shopUI.Initialize(Context);
        if (actionMenuUI != null) actionMenuUI.Initialize(Context);
        if (battleUI != null) battleUI.Initialize(Context);

        Debug.Log("<color=green>[TowersFeature] Đã khởi tạo hoàn tất hệ thống Tháp & Battle UI!</color>");
    }

    private void Update()
    {
        // Kiểm tra chuột theo New Input System
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        // Khi người chơi bấm chuột trái
        if (mouse.leftButton.wasPressedThisFrame)
        {
            // Bỏ qua nếu đang click trên giao diện UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            DetectSlotClick(mouse.position.ReadValue());
        }
    }

    private void DetectSlotClick(Vector2 screenPosition)
    {
        if (Context == null || Context.Slots == null || Camera.main == null) return;

        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(screenPosition);

        // Quét các ô tháp trong Context
        foreach (var slot in Context.Slots)
        {
            if (slot == null || slot.SlotKind != DefenseBuildingSlot.Kind.Tower) continue;

            // Tăng bán kính lên 1.2f để click dễ trúng ô hơn
            if (Vector2.Distance(mouseWorld, slot.transform.position) <= 1.2f)
            {
                OnSlotInteracted(slot);
                break;
            }
        }
    }

    public void OnSlotInteracted(DefenseBuildingSlot slot)
    {
        if (slot.Occupant == null)
        {
            // Ô trống -> Xây tháp từ Shop
            if (shopUI != null) shopUI.HandleSlotClick(slot);
        }
        else
        {
            // Ô đã có tháp -> Mở Menu Nâng cấp / Bán
            if (slot.Occupant.TryGetComponent<TowerController>(out var tower))
            {
                if (actionMenuUI != null) actionMenuUI.OpenMenu(tower, slot);
            }
        }
    }
}