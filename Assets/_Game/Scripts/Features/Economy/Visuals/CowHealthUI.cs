using TMPro;
using UnityEngine;

/// <summary>
/// Nhãn trạng thái bệnh nổi trên đầu bò.
/// Nếu chưa được gán nhãn trong Inspector, nhãn sẽ được tạo lúc chạy để luôn hiển thị.
/// </summary>
[DisallowMultipleComponent]
public class CowHealthUI : MonoBehaviour
{
    [Header("Tham chiếu")]
    [SerializeField] private CowHealth cowHealth;
    [SerializeField] private TextMeshPro statusLabel; // World-space TextMeshPro

    [Header("Hiển thị")]
    [SerializeField] private float labelHeight = 0.75f;
    [SerializeField] private float labelFontSize = 2f;

    private void Awake()
    {
        ResolveCowHealth();
        EnsureLabel();
        Refresh();
    }

    private void OnEnable()
    {
        ResolveCowHealth();
        if (cowHealth != null)
        {
            cowHealth.OnDiseaseChanged += Refresh;
        }
        Refresh();
    }

    private void OnDisable()
    {
        if (cowHealth != null)
        {
            cowHealth.OnDiseaseChanged -= Refresh;
        }
    }

    private void ResolveCowHealth()
    {
        if (cowHealth != null) return;

        cowHealth = GetComponent<CowHealth>();
        if (cowHealth == null)
        {
            cowHealth = GetComponentInParent<CowHealth>();
        }
        if (cowHealth == null)
        {
            cowHealth = GetComponentInChildren<CowHealth>();
        }
    }

    /// <summary>
    /// Tạo nhãn world-space nếu prefab chưa có sẵn.
    /// </summary>
    private void EnsureLabel()
    {
        if (statusLabel != null) return;

        var labelObject = new GameObject("StatusLabel", typeof(TextMeshPro));
        labelObject.transform.SetParent(transform, false);
        labelObject.transform.localPosition = new Vector3(0f, labelHeight, 0f);

        var label = labelObject.GetComponent<TextMeshPro>();
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = labelFontSize;
        label.color = Color.white;
        label.raycastTarget = false;
        

        if (TMP_Settings.defaultFontAsset != null)
        {
            label.font = TMP_Settings.defaultFontAsset;
        }

        statusLabel = label;
        labelObject.SetActive(false);
    }

    public void Refresh()
    {
        if (statusLabel == null) return;
        if (cowHealth == null) ResolveCowHealth();
        if (cowHealth == null) return;

        DiseaseLevel disease = cowHealth.CurrentDisease;

        if (disease == DiseaseLevel.Healthy)
        {
            statusLabel.gameObject.SetActive(false);
            return;
        }

        statusLabel.gameObject.SetActive(true);

        switch (disease)
        {
            case DiseaseLevel.Mild:
                statusLabel.text = "Bệnh nhẹ (x1.5s)";
                statusLabel.color = Color.yellow;
                break;

            case DiseaseLevel.Moderate:
                statusLabel.text = "Bệnh vừa (x2s, 50% sữa)";
                statusLabel.color = new Color(1f, 0.5f, 0f); // Orange
                break;

            case DiseaseLevel.Severe:
                statusLabel.text = "Bệnh nặng (Sữa hỏng)";
                statusLabel.color = Color.red;
                break;
        }
    }
}
