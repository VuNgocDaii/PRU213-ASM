using System.Reflection;
using UnityEngine;

[DisallowMultipleComponent]
public class CowInteraction : Interactable
{
    [Header("Tham chiếu")]
    [SerializeField] private CowController cowController;

    private static readonly FieldInfo InteractionTextField =
        typeof(Interactable).GetField("interactionText", BindingFlags.NonPublic | BindingFlags.Instance);

    private void Awake()
    {
        if (cowController == null)
        {
            cowController = GetComponent<CowController>();
        }
        UpdateHintText();
    }

    private void Update()
    {
        UpdateHintText();
    }

    public override void Interact()
    {
        if (cowController == null)
        {
            Debug.LogWarning("[CowInteraction] Chưa gán CowController.", this);
            return;
        }

        switch (cowController.CurrentState)
        {
            case CowState.Idle:
                cowController.TryFeed();
                break;
            case CowState.Producing:
                Debug.Log($"[CowInteraction] Bò đang nhai cỏ và tạo sữa. Còn lại: {cowController.RemainingTime:F1}s.", this);
                break;
            case CowState.Ready:
                cowController.TryCollectMilk();
                break;
        }

        UpdateHintText();
    }

    public new string GetInteractionText()
    {
        return BuildCurrentText();
    }

    private void UpdateHintText()
    {
        if (InteractionTextField != null)
        {
            InteractionTextField.SetValue(this, BuildCurrentText());
        }
    }

    private string BuildCurrentText()
    {
        if (cowController == null)
        {
            return "Kiểm tra bò";
        }

        switch (cowController.CurrentState)
        {
            case CowState.Idle:
                return $"Cho bò ăn ({cowController.RequiredFeedCost} Feed)";
            case CowState.Producing:
                return $"Đang tạo sữa ({cowController.RemainingTime:F0}s)";
            case CowState.Ready:
                return $"Thu hoạch sữa (+{cowController.LockedYield} RawMilk)";
            default:
                return "Tương tác";
        }
    }
}
