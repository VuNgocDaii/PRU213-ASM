using System;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [SerializeField, Min(0)] private int credits = 100;
    [SerializeField, Min(0)] private int feed = 0;
    [SerializeField, Min(0)] private int rawMilk = 0;
    [SerializeField, Min(0)] private int bottledMilk = 0;
    [SerializeField, Min(0)] private int medicine = 0;
    [SerializeField, Min(0)] private int spoiledMilk = 0;

    // Các tính năng khác có thể đăng ký để cập nhật giao diện.
    public event Action OnChanged;

    public int GetAmount(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Credits:
                return credits;
            case ItemType.Feed:
                return feed;
            case ItemType.RawMilk:
                return rawMilk;
            case ItemType.BottledMilk:
                return bottledMilk;
            case ItemType.Medicine:
                return medicine;
            case ItemType.SpoiledMilk:
                return spoiledMilk;
            default:
                return 0;
        }
    }

    public void Add(ItemType itemType, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        // Từ chối phép cộng vượt giới hạn int để tránh tràn thành số âm.
        int currentAmount = GetAmount(itemType);
        if (amount > int.MaxValue - currentAmount)
        {
            return;
        }

        switch (itemType)
        {
            case ItemType.Credits:
                credits += amount;
                break;
            case ItemType.Feed:
                feed += amount;
                break;
            case ItemType.RawMilk:
                rawMilk += amount;
                break;
            case ItemType.BottledMilk:
                bottledMilk += amount;
                break;
            case ItemType.Medicine:
                medicine += amount;
                break;
            case ItemType.SpoiledMilk:
                spoiledMilk += amount;
                break;
            default:
                return;
        }

        // Chỉ thông báo sau khi số lượng thực sự thay đổi.
        if (OnChanged != null)
        {
            OnChanged();
        }
    }

    public bool TryRemove(ItemType itemType, int amount)
    {
        if (amount <= 0)
        {
            return false;
        }

        int currentAmount = GetAmount(itemType);
        if (currentAmount < amount)
        {
            return false;
        }

        switch (itemType)
        {
            case ItemType.Credits:
                credits -= amount;
                break;
            case ItemType.Feed:
                feed -= amount;
                break;
            case ItemType.RawMilk:
                rawMilk -= amount;
                break;
            case ItemType.BottledMilk:
                bottledMilk -= amount;
                break;
            case ItemType.Medicine:
                medicine -= amount;
                break;
            case ItemType.SpoiledMilk:
                spoiledMilk -= amount;
                break;
            default:
                return false;
        }

        if (OnChanged != null)
        {
            OnChanged();
        }

        return true;
    }

    private void OnValidate()
    {
        // Ngăn số âm khi chỉnh dữ liệu trong Inspector.
        credits = Mathf.Max(0, credits);
        feed = Mathf.Max(0, feed);
        rawMilk = Mathf.Max(0, rawMilk);
        bottledMilk = Mathf.Max(0, bottledMilk);
        medicine = Mathf.Max(0, medicine);
        spoiledMilk = Mathf.Max(0, spoiledMilk);
    }
}
