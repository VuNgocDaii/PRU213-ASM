using UnityEngine;

// A stationary, axis-aligned rectangle in world space. Keep it under the barn, never the cow.
[DisallowMultipleComponent]
public class RoamingArea : MonoBehaviour
{
    [SerializeField] private Vector2 size = new Vector2(4f, 3f);
    [SerializeField] private Vector2 centerOffset;

    public Bounds WorldBounds
    {
        get { return new Bounds(transform.position + (Vector3)centerOffset,
            new Vector3(Mathf.Max(0f, size.x), Mathf.Max(0f, size.y), 0f)); }
    }

    // padding is the world-space half-size of the cow's collider, including any offset.
    public Vector2 ClampPosition(Vector2 position, Vector2 padding)
    {
        Bounds bounds = WorldBounds;
        Vector2 half = new Vector2(Mathf.Max(0f, bounds.extents.x - Mathf.Abs(padding.x)),
            Mathf.Max(0f, bounds.extents.y - Mathf.Abs(padding.y)));
        Vector2 center = bounds.center;
        return new Vector2(Mathf.Clamp(position.x, center.x - half.x, center.x + half.x),
            Mathf.Clamp(position.y, center.y - half.y, center.y + half.y));
    }

    public bool Contains(Vector2 position, Vector2 padding)
    {
        Bounds bounds = WorldBounds;
        padding = new Vector2(Mathf.Abs(padding.x), Mathf.Abs(padding.y));
        if (padding.x > bounds.extents.x || padding.y > bounds.extents.y) return false;
        // Compare stored float coordinates returned by the same clamp operation.
        // Recomputing the endpoints in a comparison can retain extra precision on
        // Mono and reject a boundary rounded to float by ClampPosition.
        Vector2 clamped = ClampPosition(position, padding);
        return position.x == clamped.x && position.y == clamped.y;
    }

    public Vector2 GetRandomPoint(Vector2 padding)
    {
        Bounds bounds = WorldBounds;
        Vector2 half = new Vector2(Mathf.Max(0f, bounds.extents.x - Mathf.Abs(padding.x)),
            Mathf.Max(0f, bounds.extents.y - Mathf.Abs(padding.y)));
        return (Vector2)bounds.center + new Vector2(Random.Range(-half.x, half.x), Random.Range(-half.y, half.y));
    }

    private void OnValidate()
    {
        size = new Vector2(Mathf.Max(0f, size.x), Mathf.Max(0f, size.y));
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(WorldBounds.center, WorldBounds.size);
    }
}
