using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ExpandedMapCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private MapZone[] zones;
    [SerializeField, Min(0.01f)] private float followSpeed = 7;
    private Camera view;
    private void Awake() { view = GetComponent<Camera>(); }
    private void LateUpdate()
    {
        if (target == null || zones == null) return;
        Bounds bounds = default;
        bool found = false;
        foreach (MapZone zone in zones)
        {
            if (zone == null || !zone.IsUnlocked) continue;
            if (!found) { bounds = zone.WorldBounds; found = true; } else bounds.Encapsulate(zone.WorldBounds);
        }
        if (!found) return;
        float halfY = view.orthographicSize, halfX = halfY * view.aspect;
        Vector3 desired = new Vector3(
            bounds.size.x <= halfX * 2 ? bounds.center.x : Mathf.Clamp(target.position.x, bounds.min.x + halfX, bounds.max.x - halfX),
            bounds.size.y <= halfY * 2 ? bounds.center.y : Mathf.Clamp(target.position.y, bounds.min.y + halfY, bounds.max.y - halfY), transform.position.z);
        transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-followSpeed * Time.deltaTime));
    }
}
