using UnityEngine;

// Shipping-bin lid follows the existing transaction; the drone keeps its own flight animation.
public class DeliveryStationVisual : MonoBehaviour
{
    [SerializeField] private OrderController orders;
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private Sprite[] frames;
    private float openedTime;
    private void LateUpdate()
    {
        if (orders == null || body == null || frames == null || frames.Length < 4) return;
        if (orders.IsDelivering) openedTime += Time.deltaTime;
        else openedTime = 0;
        body.sprite = frames[orders.IsDelivering ? Mathf.Min(3,(int)(openedTime * 10)) : 0];
    }
}
