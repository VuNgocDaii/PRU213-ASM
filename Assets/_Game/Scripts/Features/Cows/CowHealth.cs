using System;
using UnityEngine;

public enum DiseaseLevel
{
    Healthy,
    Mild,
    Moderate,
    Severe
}

[DisallowMultipleComponent]
public class CowHealth : MonoBehaviour, ICowDiseaseTarget
{
    [Header("Trạng thái")]
    [SerializeField] private DiseaseLevel currentDisease = DiseaseLevel.Healthy;

    [Header("Cấu hình leo thang bệnh nếu không chữa")]
    [SerializeField] private bool autoEscalate = true;
    [SerializeField, Min(5f)] private float escalationDuration = 40f; // 30 - 45s theo tài liệu thiết kế
    [SerializeField] private float escalationTimer = 0f;

    // --- ICowDiseaseTarget ---
    public Transform TargetTransform => transform;
    public bool IsHealthy => currentDisease == DiseaseLevel.Healthy;
    public bool CanBeInfected => currentDisease < DiseaseLevel.Severe;

    private void Update()
    {
        // Bệnh không chữa sẽ tự leo thang sau khoảng 30-45 giây
        if (autoEscalate && currentDisease > DiseaseLevel.Healthy && currentDisease < DiseaseLevel.Severe)
        {
            escalationTimer += Time.deltaTime;
            if (escalationTimer >= escalationDuration)
            {
                escalationTimer = 0f;
                Debug.Log($"[CowHealth] {name}: Bệnh không được chữa trị kịp thời đã tự leo thang!", this);
                Infect();
            }
        }
        else if (currentDisease == DiseaseLevel.Healthy || currentDisease == DiseaseLevel.Severe)
        {
            escalationTimer = 0f;
        }
    }

    public void Infect()
    {
        if (!CanBeInfected) return;
        currentDisease++;
        milkAccumulator = 0f; // Reset accumulator khi chuyển cấp bệnh
        escalationTimer = 0f;
        OnDiseaseChanged?.Invoke();
        Debug.Log($"[CowHealth] {name} bị nhiễm bệnh → {currentDisease}", this);
    }

    // --- Public API ---
    public DiseaseLevel CurrentDisease => currentDisease;
    public float EscalationTimer => escalationTimer;
    public float EscalationDuration => escalationDuration;
    public event Action OnDiseaseChanged;

    /// <summary>
    /// Hệ số nhân thời gian sản xuất theo mức bệnh.
    /// Healthy: 1.0, Mild: 1.5, Moderate: 2.0, Severe: 2.0.
    /// </summary>
    public float DurationMultiplier
    {
        get
        {
            switch (currentDisease)
            {
                case DiseaseLevel.Mild:
                    return 1.5f;
                case DiseaseLevel.Moderate:
                case DiseaseLevel.Severe:
                    return 2.0f;
                default:
                    return 1.0f;
            }
        }
    }

    // --- Fractional Accumulator (Moderate) ---
    private float milkAccumulator = 0f;
    public float MilkAccumulator => milkAccumulator;

    /// <summary>
    /// Cộng amount vào bộ tích lũy. Trả số RawMilk có thể thu ngay (0 hoặc 1).
    /// Phần dư giữ lại cho lượt tiếp theo.
    /// </summary>
    public int AccumulateMilk(float amount)
    {
        milkAccumulator += amount;
        int earned = Mathf.FloorToInt(milkAccumulator);
        milkAccumulator -= earned;
        return earned;
    }

    /// <summary>
    /// Dùng 1 Medicine từ kho để chữa bớt một cấp bệnh.
    /// Severe → Moderate ; Moderate và Mild → Healthy. Trả true nếu thành công.
    /// </summary>
    public bool TryHeal(InventoryManager inventory)
    {
        if (inventory == null || currentDisease == DiseaseLevel.Healthy) return false;
        if (!inventory.TryRemove(ItemType.Medicine, 1)) return false;

        var before = currentDisease;

        if (currentDisease == DiseaseLevel.Severe)
            currentDisease = DiseaseLevel.Moderate;
        else
            currentDisease = DiseaseLevel.Healthy;

        milkAccumulator = 0f;
        escalationTimer = 0f;
        OnDiseaseChanged?.Invoke();
        Debug.Log($"[CowHealth] {name}: {before} → {currentDisease} (dùng 1 Medicine)", this);
        return true;
    }

    // --- Context Registration ---
    private DefenseSceneContext registeredContext;

    public void RegisterWith(DefenseSceneContext context)
    {
        if (registeredContext == context) return;
        Unregister();
        registeredContext = context;
        if (context != null)
        {
            context.RegisterCow(this);
        }
    }

    public void Unregister()
    {
        if (registeredContext != null)
        {
            registeredContext.UnregisterCow(this);
            registeredContext = null;
        }
    }

    private void OnDestroy()
    {
        Unregister();
    }

    // --- Testing Context Menus (Click chuột phải vào component trong Inspector khi Play Mode) ---
    [ContextMenu("Test/Infect (Lây bệnh)")]
    public void TestInfect()
    {
        Infect();
    }

    [ContextMenu("Test/Heal (Chữa khỏi)")]
    public void TestHeal()
    {
        currentDisease = DiseaseLevel.Healthy;
        milkAccumulator = 0f;
        escalationTimer = 0f;
        OnDiseaseChanged?.Invoke();
        Debug.Log($"[CowHealth] {name} đã được reset về Healthy.", this);
    }

    [ContextMenu("Test/Try Heal With Inventory (Use 1 Medicine)")]
    public void TestTryHealWithInventory()
    {
        var inv = InventoryManager.Instance;
        if (inv != null)
        {
            bool success = TryHeal(inv);
            if (!success)
            {
                Debug.LogWarning($"[CowHealth] Không thể chữa bệnh: Medicine hiện có = {inv.GetAmount(ItemType.Medicine)}, Trạng thái = {currentDisease}");
            }
        }
        else
        {
            Debug.LogError("[CowHealth] Không tìm thấy InventoryManager trong Scene!");
        }
    }

    [ContextMenu("Test/Set Mild (x1.5s)")]
    public void TestSetMild()
    {
        currentDisease = DiseaseLevel.Mild;
        milkAccumulator = 0f;
        escalationTimer = 0f;
        OnDiseaseChanged?.Invoke();
        Debug.Log($"[CowHealth] {name} đặt thành Mild.", this);
    }

    [ContextMenu("Test/Set Moderate (x2s, 50% sữa)")]
    public void TestSetModerate()
    {
        currentDisease = DiseaseLevel.Moderate;
        milkAccumulator = 0f;
        escalationTimer = 0f;
        OnDiseaseChanged?.Invoke();
        Debug.Log($"[CowHealth] {name} đặt thành Moderate.", this);
    }

    [ContextMenu("Test/Set Severe (Sữa hỏng)")]
    public void TestSetSevere()
    {
        currentDisease = DiseaseLevel.Severe;
        milkAccumulator = 0f;
        escalationTimer = 0f;
        OnDiseaseChanged?.Invoke();
        Debug.Log($"[CowHealth] {name} đặt thành Severe.", this);
    }
}
