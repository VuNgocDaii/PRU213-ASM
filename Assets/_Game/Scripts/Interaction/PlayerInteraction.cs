using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PlayerInteraction : MonoBehaviour
{
    [SerializeField, Min(0f)] private float interactionRadius = 1.5f;
    [SerializeField] private LayerMask interactionLayerMask = ~0;
    [SerializeField] private TextMeshProUGUI interactionHint;

    private Interactable currentTarget;
    private readonly List<Interactable> checkedTargets = new List<Interactable>();

    private void Update()
    {
        FindNearestTarget();
        UpdateHint();

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.eKey.wasPressedThisFrame && currentTarget != null)
        {
            // Chỉ gọi một lần lúc vừa nhấn, không gọi liên tục khi giữ E.
            currentTarget.Interact();
            // Mục tiêu có thể bị tắt hoặc hủy sau khi tương tác.
            FindNearestTarget();
            UpdateHint();
        }
    }

    private void FindNearestTarget()
    {
        currentTarget = null;
        checkedTargets.Clear();
        Vector2 playerPosition = transform.position;
        float nearestDistanceSquared = interactionRadius * interactionRadius;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(
            playerPosition, interactionRadius, interactionLayerMask);

        for (int index = 0; index < colliders.Length; index++)
        {
            // Collider có thể nằm ở con, Interactable nằm ở cha.
            Interactable target = colliders[index].GetComponentInParent<Interactable>();
            if (target == null || !target.isActiveAndEnabled || checkedTargets.Contains(target))
            {
                continue;
            }
            checkedTargets.Add(target);

            // Tính khoảng cách đến vị trí đối tượng, không đếm số collider.
            Vector2 targetPosition = target.transform.position;
            float distanceSquared = (targetPosition - playerPosition).sqrMagnitude;
            if (distanceSquared <= nearestDistanceSquared)
            {
                nearestDistanceSquared = distanceSquared;
                currentTarget = target;
            }
        }
    }

    private void UpdateHint()
    {
        if (interactionHint == null)
        {
            return;
        }

        if (currentTarget != null)
        {
            interactionHint.text = "Nhấn E: " + currentTarget.GetInteractionText();
            interactionHint.gameObject.SetActive(true);
        }
        else
        {
            interactionHint.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        currentTarget = null;
        UpdateHint();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
