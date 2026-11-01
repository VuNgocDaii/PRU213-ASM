using UnityEngine;

public class InventoryTester : MonoBehaviour
{
    [SerializeField] private InventoryManager inventory;

    private int changeCount = 0;

    private void Start()
    {
        if (inventory == null)
        {
            Debug.LogError("Chưa gắn InventoryManager vào ô Inventory.");
            return;
        }

        inventory.OnChanged += CountChanges;

        // Kiểm tra số lượng ban đầu.
        Debug.Assert(inventory.GetAmount(ItemType.Credits) == 100,
            "Credits ban đầu phải bằng 100.");

        Debug.Assert(inventory.GetAmount(ItemType.Feed) == 0,
            "Feed ban đầu phải bằng 0.");

        // Thêm hợp lệ: Feed từ 0 lên 10.
        inventory.Add(ItemType.Feed, 10);
        Debug.Assert(inventory.GetAmount(ItemType.Feed) == 10,
            "Thêm Feed thất bại.");

        // Trừ hợp lệ: Feed từ 10 còn 6.
        bool removed = inventory.TryRemove(ItemType.Feed, 4);
        Debug.Assert(removed, "Trừ Feed phải thành công.");
        Debug.Assert(inventory.GetAmount(ItemType.Feed) == 6,
            "Feed phải còn 6.");

        // Không đủ tài nguyên: giữ nguyên 6.
        bool removedTooMuch = inventory.TryRemove(ItemType.Feed, 20);
        Debug.Assert(!removedTooMuch, "Không được trừ quá số lượng đang có.");

        // Số lượng không hợp lệ: không được thay đổi kho.
        inventory.Add(ItemType.Feed, 0);
        inventory.Add(ItemType.Feed, -5);

        bool removedZero = inventory.TryRemove(ItemType.Feed, 0);
        bool removedNegative = inventory.TryRemove(ItemType.Feed, -2);

        Debug.Assert(!removedZero, "Phải từ chối trừ 0.");
        Debug.Assert(!removedNegative, "Phải từ chối trừ số âm.");
        Debug.Assert(inventory.GetAmount(ItemType.Feed) == 6,
            "Các thao tác bị từ chối phải giữ nguyên Feed.");

        // Chỉ hai thao tác hợp lệ được phát sự kiện.
        Debug.Assert(changeCount == 2,
            "OnChanged phải được gọi đúng 2 lần.");

        inventory.OnChanged -= CountChanges;

        Debug.Log("Đã chạy xong test kho. Nếu không có lỗi Assert thì các kiểm tra đã đạt.");
    }

    private void CountChanges()
    {
        changeCount++;
    }
}