using UnityEngine;

public class OrderInteraction : Interactable
{
    [SerializeField] private OrderController orderController;

    public override void Interact()
    {
        if (orderController != null)
        {
            orderController.TryDeliverOrder();
        }
        else
        {
            Debug.LogError("OrderInteraction: Chưa gán OrderController vào ô Inspector!", this);
        }
    }
}