using TMPro;
using UnityEngine;

// Máy đóng chai sữa.
// Nhấn E lần 1: đủ RawMilk thì trừ và bắt đầu đóng chai.
// Có điện thì đếm giờ; mất điện thì dừng lại, giữ nguyên tiến độ.
// Xong thì nhấn E lần 2 để nhận BottledMilk.
public class ProductionMachine : Interactable
{
    [Header("Tham chiếu")]
    [SerializeField] private FeatureReferences featureReferences;
    [SerializeField] private PowerSource powerSource;
    [SerializeField] private TextMeshPro statusText;      // chữ trạng thái phía trên máy
    [SerializeField] private Transform progressFill;      // thanh tiến độ: scale X từ 0 đến 1

    [Header("Cấu hình cấp 1")]
    [SerializeField, Min(1)] private int rawMilkCost = 2;          // RawMilk mỗi lượt
    [SerializeField, Min(1)] private int bottledMilkAmount = 1;    // BottledMilk mỗi lượt
    [SerializeField, Min(0.1f)] private float processTime = 5f;    // số giây mỗi lượt

    // Chỉ số nâng cấp do MachineUpgrade đặt, áp dụng từ lượt sau
    private float timeMultiplier = 1f;
    private int amountMultiplier = 1;

    // Trạng thái của lượt đang chạy
    private bool isProcessing;
    private bool isReady;
    private float duration;        // thời gian của lượt này (chốt lúc bắt đầu)
    private float remainingTime;
    private int outputAmount;      // số chai của lượt này (chốt lúc bắt đầu)

    public bool IsProcessing => isProcessing;
    public bool IsReady => isReady;
    public bool HasPower => powerSource != null && powerSource.HasPower;

    // Thông báo ngắn như "Thiếu RawMilk", hiện trong 2 giây
    private string message;
    private float messageTimer;

    public override void Interact()
    {
        if (featureReferences == null || featureReferences.Inventory == null)
        {
            Debug.LogWarning(name + ": Chưa Initialize FeatureReferences, chưa cho dùng máy.", this);
            return;
        }

        if (isReady)
        {
            Collect();
        }
        else if (isProcessing)
        {
            ShowMessage("Máy đang chạy, chờ xong lượt này");
        }
        else
        {
            StartProcessing();
        }
    }

    private void StartProcessing()
    {
        if (powerSource == null || !powerSource.HasPower)
        {
            ShowMessage("Mất điện, chưa chạy được");
            return;
        }

        // Chỉ bắt đầu khi trừ RawMilk thành công
        InventoryManager inventory = featureReferences.Inventory;
        if (!inventory.TryRemove(ItemType.RawMilk, rawMilkCost))
        {
            ShowMessage("Thiếu RawMilk (cần " + rawMilkCost + ")");
            return;
        }

        // Chốt thời gian và sản lượng cho lượt này: nâng cấp giữa chừng không làm đổi
        duration = processTime * timeMultiplier;
        remainingTime = duration;
        outputAmount = bottledMilkAmount * amountMultiplier;
        isProcessing = true;
    }

    private void Update()
    {
        // Chỉ đếm giờ khi đang chạy và có điện
        if (isProcessing && powerSource != null && powerSource.HasPower)
        {
            remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);
            if (remainingTime <= 0f)
            {
                isProcessing = false;
                isReady = true;
            }
        }

        if (messageTimer > 0f)
        {
            messageTimer -= Time.deltaTime;
        }

        UpdateDisplay();
    }

    private void Collect()
    {
        InventoryManager inventory = featureReferences.Inventory;

        // Add không báo thất bại, nên tự kiểm tra tràn int trước khi cộng
        if (inventory.GetAmount(ItemType.BottledMilk) > int.MaxValue - outputAmount)
        {
            ShowMessage("Kho BottledMilk đã đầy");
            return;
        }

        inventory.Add(ItemType.BottledMilk, outputAmount);
        ShowMessage("+" + outputAmount + " BottledMilk");
        isReady = false;
    }

    // MachineUpgrade gọi hàm này khi nâng cấp
    public void SetUpgradeStats(float newTimeMultiplier, int newAmountMultiplier)
    {
        if (newTimeMultiplier <= 0f || newAmountMultiplier < 1)
        {
            Debug.LogWarning(name + ": Chỉ số nâng cấp không hợp lệ.", this);
            return;
        }

        timeMultiplier = newTimeMultiplier;
        amountMultiplier = newAmountMultiplier;
    }

    private void ShowMessage(string text)
    {
        message = text;
        messageTimer = 2f;
    }

    private void UpdateDisplay()
    {
        if (progressFill != null)
        {
            float progress = 0f;
            if (isProcessing)
            {
                progress = 1f - remainingTime / duration;
            }
            else if (isReady)
            {
                progress = 1f;
            }
            progressFill.localScale = new Vector3(progress, 1f, 1f);
        }

        if (statusText == null)
        {
            return;
        }

        bool hasPower = powerSource != null && powerSource.HasPower;
        if (messageTimer > 0f)
        {
            statusText.text = message;
            statusText.color = Color.yellow;
        }
        else if (isReady)
        {
            statusText.text = "Xong! Nhấn E nhận " + outputAmount + " chai";
            statusText.color = Color.green;
        }
        else if (isProcessing && !hasPower)
        {
            statusText.text = "MẤT ĐIỆN - tạm dừng (" + remainingTime.ToString("0.0") + "s)";
            statusText.color = Color.red;
        }
        else if (isProcessing)
        {
            statusText.text = "Đang đóng chai... " + remainingTime.ToString("0.0") + "s";
            statusText.color = Color.white;
        }
        else
        {
            statusText.text = "Cần " + rawMilkCost + " RawMilk";
            statusText.color = Color.white;
        }
    }
}
