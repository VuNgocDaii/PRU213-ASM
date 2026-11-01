using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class WorldDepthSorting : MonoBehaviour
{
    [SerializeField] private float feetOffsetY;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        // Chân ở thấp hơn thì vẽ phía trước. Nền giữ order -100.
        float feetY = transform.position.y + feetOffsetY;
        spriteRenderer.sortingOrder = GetSortingOrder(feetY);
    }

    public static int GetSortingOrder(float feetY)
    {
        return 1000 - Mathf.RoundToInt(feetY * 100f);
    }
}
