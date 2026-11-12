using UnityEngine;

public static class BuildingFootprintUtility
{
    public static bool IsFootprintClear(Vector2 center, Vector2 size, Transform ignoreRoot = null, LayerMask? mask = null)
    {
        if (size.x <= 0f || size.y <= 0f) return true;

        LayerMask queryMask = mask ?? ~0;
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f, queryMask);
        if (hits == null || hits.Length == 0) return true;

        foreach (Collider2D hit in hits)
        {
            if (hit == null) continue;
            // Triggers like zones, slot interaction areas, roaming areas do not block building footprint
            if (hit.isTrigger) continue;
            // The player must approach a pad to use it; their body is not a placement obstacle.
            if (hit.GetComponentInParent<PlayerMovement>() != null) continue;
            // Ignore objects under the current slot or being inspected
            if (ignoreRoot != null && (hit.transform == ignoreRoot || hit.transform.IsChildOf(ignoreRoot))) continue;

            // Found a solid blocking obstacle (e.g. rocks, fences, other structures)
            return false;
        }

        return true;
    }
}
