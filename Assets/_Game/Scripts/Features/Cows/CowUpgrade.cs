using UnityEngine;

[DisallowMultipleComponent]
public class CowUpgrade : UpgradeTarget
{
    [Header("Cấu hình cấp độ")]
    [SerializeField, Min(1)] private int currentLevel = 1;
    [SerializeField, Min(0.1f)] private float baseDuration = 10f;
    [SerializeField, Min(1)] private int baseYield = 1;
    [SerializeField, Min(1)] private int feedCost = 1;

    public int CurrentLevel => currentLevel;
    public int FeedCost => feedCost;

    public float ProductionDuration => GetDurationForLevel(currentLevel);
    public int MilkYield => GetYieldForLevel(currentLevel);

    public override void ApplyLevel(int level)
    {
        base.ApplyLevel(level);

        if (level < 1 || level > 3)
        {
            Debug.LogWarning($"[CowUpgrade] Cấp độ {level} không được hỗ trợ (chỉ từ 1 đến 3).", this);
            return;
        }

        // Đặt cấp tuyệt đối (idempotent), không nhân dồn mỗi lần gọi
        currentLevel = level;
        Debug.Log($"[CowUpgrade] Đã áp dụng cấp {currentLevel}: Thời gian={ProductionDuration:F1}s, Sản lượng={MilkYield} RawMilk", this);
    }

    public float GetDurationForLevel(int level)
    {
        switch (level)
        {
            case 1:
                return baseDuration;
            case 2:
                return baseDuration * 0.8f;
            case 3:
                return baseDuration * 0.6f;
            default:
                return baseDuration;
        }
    }

    public int GetYieldForLevel(int level)
    {
        switch (level)
        {
            case 1:
            case 2:
                return baseYield;
            case 3:
                return baseYield * 2;
            default:
                return baseYield;
        }
    }

    private void OnValidate()
    {
        currentLevel = Mathf.Clamp(currentLevel, 1, 3);
        baseDuration = Mathf.Max(0.1f, baseDuration);
        baseYield = Mathf.Max(1, baseYield);
        feedCost = Mathf.Max(1, feedCost);
    }
}
