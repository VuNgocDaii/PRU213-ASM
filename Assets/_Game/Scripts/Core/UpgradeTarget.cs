using UnityEngine;

public class UpgradeTarget : MonoBehaviour
{
    // Đặt cấp tuyệt đối, không cộng thêm cấp/chỉ số mỗi lần gọi.
    public virtual void ApplyLevel(int level)
    {
        if (level < 1)
        {
            Debug.LogWarning("Cấp nâng cấp phải bắt đầu từ 1.", this);
            return;
        }
    }
}
