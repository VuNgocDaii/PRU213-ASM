using UnityEngine;

public class UpgradeTargetSample : UpgradeTarget
{
    [SerializeField, Min(1)] private int currentLevel = 1;

    public int CurrentLevel
    {
        get { return currentLevel; }
    }

    public override void ApplyLevel(int level)
    {
        if (level < 1)
        {
            Debug.LogWarning("Cấp nâng cấp phải bắt đầu từ 1. Giữ nguyên cấp hiện tại.", this);
            return;
        }

        currentLevel = level;
        Debug.Log(name + ": Đã đặt cấp tuyệt đối = " + currentLevel, this);
    }
}
