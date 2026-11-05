using UnityEngine;

// Nhận nâng cấp từ phần Xây dựng (Building gọi ApplyLevel sau khi trừ Credits).
// Cấp 1: như gốc. Cấp 2: thời gian x0.8. Cấp 3: thời gian x0.6, sản lượng x2.
[DisallowMultipleComponent]
public class MachineUpgrade : UpgradeTarget
{
    [SerializeField] private ProductionMachine machine;
    [SerializeField] private float[] timeMultipliers = { 1f, 0.8f, 0.6f };
    [SerializeField] private int[] amountMultipliers = { 1, 1, 2 };

    private int currentLevel = 1;

    public int CurrentLevel
    {
        get { return currentLevel; }
    }

    public int MaxLevel
    {
        get { return Mathf.Min(timeMultipliers.Length, amountMultipliers.Length); }
    }

    // Đặt cấp tuyệt đối: gọi cấp 2 hai lần vẫn là cấp 2
    public override void ApplyLevel(int level)
    {
        if (level < 1 || level > MaxLevel)
        {
            Debug.LogWarning(name + ": Máy chỉ hỗ trợ cấp 1 đến " + MaxLevel + ".", this);
            return;
        }

        if (machine == null)
        {
            Debug.LogWarning(name + ": Chưa gán ProductionMachine.", this);
            return;
        }

        currentLevel = level;
        machine.SetUpgradeStats(timeMultipliers[level - 1], amountMultipliers[level - 1]);
    }

    // Thử nhanh khi Play: chuột phải component trong Inspector
    [ContextMenu("Test: Level 1")]
    private void TestLevel1() { ApplyLevel(1); }

    [ContextMenu("Test: Level 3")]
    private void TestLevel3() { ApplyLevel(3); }
}
