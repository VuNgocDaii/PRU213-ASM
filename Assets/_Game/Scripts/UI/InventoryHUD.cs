using TMPro;
using UnityEngine;

public class InventoryHUD : MonoBehaviour
{
    [SerializeField] private InventoryManager inventory;
    [SerializeField] private TextMeshProUGUI creditsText;
    [SerializeField] private TextMeshProUGUI feedText;
    [SerializeField] private TextMeshProUGUI rawMilkText;
    [SerializeField] private TextMeshProUGUI bottledMilkText;

    private void OnEnable()
    {
        if (!CheckReferences())
        {
            return;
        }

        // Lắng nghe thay đổi và hiển thị số lượng ngay khi bật HUD.
        inventory.OnChanged += UpdateHUD;
        UpdateHUD();
    }

    private void OnDisable()
    {
        // Ngừng lắng nghe khi HUD bị tắt hoặc bị hủy.
        if (inventory != null)
        {
            inventory.OnChanged -= UpdateHUD;
        }
    }

    private void UpdateHUD()
    {
        // Kiểm tra lại nếu một tham chiếu bị mất trong lúc chơi.
        if (!CheckReferences())
        {
            return;
        }

        // HUD chỉ hiển thị số lượng; icon được gán riêng trong Inspector.
        creditsText.text = inventory.GetAmount(ItemType.Credits).ToString();
        feedText.text = inventory.GetAmount(ItemType.Feed).ToString();
        rawMilkText.text = inventory.GetAmount(ItemType.RawMilk).ToString();
        bottledMilkText.text = inventory.GetAmount(ItemType.BottledMilk).ToString();
    }

    private bool CheckReferences()
    {
        // Báo rõ từng ô chưa được gắn trong Inspector.
        bool referencesValid = true;

        if (inventory == null)
        {
            Debug.LogError("InventoryHUD: Chưa gắn InventoryManager vào ô Inventory.", this);
            referencesValid = false;
        }

        if (creditsText == null)
        {
            Debug.LogError("InventoryHUD: Chưa gắn TextMeshProUGUI vào ô Credits Text.", this);
            referencesValid = false;
        }

        if (feedText == null)
        {
            Debug.LogError("InventoryHUD: Chưa gắn TextMeshProUGUI vào ô Feed Text.", this);
            referencesValid = false;
        }

        if (rawMilkText == null)
        {
            Debug.LogError("InventoryHUD: Chưa gắn TextMeshProUGUI vào ô Raw Milk Text.", this);
            referencesValid = false;
        }

        if (bottledMilkText == null)
        {
            Debug.LogError("InventoryHUD: Chưa gắn TextMeshProUGUI vào ô Bottled Milk Text.", this);
            referencesValid = false;
        }

        return referencesValid;
    }
}
