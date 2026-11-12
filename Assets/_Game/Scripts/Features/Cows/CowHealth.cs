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
    [SerializeField, Min(5f)] private float escalationDuration = 40f;
    [SerializeField] private float escalationTimer = 0f;

    // --- ICowDiseaseTarget ---
    public Transform TargetTransform => transform;
    public bool IsHealthy => currentDisease == DiseaseLevel.Healthy;
    public bool CanBeInfected => currentDisease < DiseaseLevel.Severe;

    public DiseaseLevel CurrentDisease => currentDisease;
    public float EscalationTimer => escalationTimer;
    public float EscalationDuration => escalationDuration;
    public event Action OnDiseaseChanged;

    public float DurationMultiplier => currentDisease switch
    {
        DiseaseLevel.Mild => 1.5f,
        DiseaseLevel.Moderate or DiseaseLevel.Severe => 2.0f,
        _ => 1.0f
    };

    private float milkAccumulator = 0f;
    public float MilkAccumulator => milkAccumulator;

    private DefenseSceneContext registeredContext;

    private void OnEnable()
    {
        var context = FindAnyObjectByType<DefenseSceneContext>();
        if (context != null) RegisterWith(context);
    }

    private void OnDisable() => Unregister();
    private void OnDestroy() => Unregister();

    private void Update()
    {
        if (autoEscalate && currentDisease > DiseaseLevel.Healthy && currentDisease < DiseaseLevel.Severe)
        {
            escalationTimer += Time.deltaTime;
            if (escalationTimer >= escalationDuration)
            {
                escalationTimer = 0f;
                Debug.Log($"[CowHealth] {name}: Bệnh tự leo thang do không chữa kịp thời!", this);
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
        milkAccumulator = 0f;
        escalationTimer = 0f;
        OnDiseaseChanged?.Invoke();
        Debug.Log($"[CowHealth] {name} bị nhiễm bệnh → {currentDisease}", this);
    }

    public int AccumulateMilk(float amount)
    {
        milkAccumulator += amount;
        int earned = Mathf.FloorToInt(milkAccumulator);
        milkAccumulator -= earned;
        return earned;
    }

    public bool TryHeal(InventoryManager inventory)
    {
        if (inventory == null || currentDisease == DiseaseLevel.Healthy) return false;
        if (!inventory.TryRemove(ItemType.Medicine, 1)) return false;

        var before = currentDisease;
        currentDisease = (currentDisease == DiseaseLevel.Severe) ? DiseaseLevel.Moderate : DiseaseLevel.Healthy;

        milkAccumulator = 0f;
        escalationTimer = 0f;
        OnDiseaseChanged?.Invoke();
        Debug.Log($"[CowHealth] {name}: {before} → {currentDisease} (dùng 1 Medicine)", this);
        return true;
    }

    public void RegisterWith(DefenseSceneContext context)
    {
        if (registeredContext == context) return;
        Unregister();
        registeredContext = context;
        if (context != null) context.RegisterCow(this);
    }

    public void Unregister()
    {
        if (registeredContext != null)
        {
            registeredContext.UnregisterCow(this);
            registeredContext = null;
        }
    }

    // --- Testing Context Menus ---
    [ContextMenu("Test/Infect")]
    public void TestInfect() => Infect();

    [ContextMenu("Test/Heal")]
    public void TestHeal()
    {
        currentDisease = DiseaseLevel.Healthy;
        milkAccumulator = 0f;
        escalationTimer = 0f;
        OnDiseaseChanged?.Invoke();
        Debug.Log($"[CowHealth] {name} reset về Healthy.", this);
    }

    [ContextMenu("Test/Set Mild")]
    public void TestSetMild() => SetDiseaseForTest(DiseaseLevel.Mild);

    [ContextMenu("Test/Set Moderate")]
    public void TestSetModerate() => SetDiseaseForTest(DiseaseLevel.Moderate);

    [ContextMenu("Test/Set Severe")]
    public void TestSetSevere() => SetDiseaseForTest(DiseaseLevel.Severe);

    private void SetDiseaseForTest(DiseaseLevel level)
    {
        currentDisease = level;
        milkAccumulator = 0f;
        escalationTimer = 0f;
        OnDiseaseChanged?.Invoke();
        Debug.Log($"[CowHealth] {name} đặt thành {level}.", this);
    }
}
