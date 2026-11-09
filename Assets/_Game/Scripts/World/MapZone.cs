using UnityEngine;

public class MapZone : MonoBehaviour
{
    [SerializeField] private CastleProgress progress;
    [SerializeField, Range(1, 3)] private int requiredLevel = 1;
    [SerializeField] private Vector2 size = new Vector2(32, 24);
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private GameObject unlockedContent;
    public int RequiredLevel => requiredLevel;
    public bool IsUnlocked => progress != null && progress.CurrentLevel >= requiredLevel;
    public Bounds WorldBounds => new Bounds(transform.position, (Vector3)size);
    private void OnEnable() { if (progress != null) progress.OnCastleLevelUp += Refresh; Refresh(0); }
    private void OnDisable() { if (progress != null) progress.OnCastleLevelUp -= Refresh; }
    private void Refresh(int level)
    {
        if (lockedOverlay != null) lockedOverlay.SetActive(!IsUnlocked);
        if (unlockedContent != null) unlockedContent.SetActive(IsUnlocked);
    }
    private void OnDrawGizmosSelected() { Gizmos.color = Color.cyan; Gizmos.DrawWireCube(WorldBounds.center, WorldBounds.size); }
}
