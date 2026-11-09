using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The existing interaction system controls the hint; this component only follows its visibility.
public class InteractionHintCard : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI hint;
    [SerializeField] private Image background;
    private void LateUpdate()
    {
        if (background != null) background.enabled = hint != null && hint.gameObject.activeInHierarchy && !string.IsNullOrEmpty(hint.text);
    }
}
