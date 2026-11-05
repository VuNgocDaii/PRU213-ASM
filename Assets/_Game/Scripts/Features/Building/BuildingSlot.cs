using UnityEngine;

[DisallowMultipleComponent]
public class BuildingSlot : Interactable
{
    [SerializeField] private BuildingController building;
    [SerializeField] private BuildingUpgradeController upgrades;
    [SerializeField] private BuildingPanel panel;
    [SerializeField] private Transform player;
    [SerializeField, Min(0.1f)] private float interactionDistance = 1.5f;

    public BuildingController Building => building;
    public BuildingUpgradeController Upgrades => upgrades;
    public bool IsPlayerInRange => player != null &&
        ((Vector2)(player.position - transform.position)).sqrMagnitude <= interactionDistance * interactionDistance;

    public override void Interact()
    {
        if (building == null || building.IsBusy || !IsPlayerInRange) return;
        if (panel == null) { Debug.LogWarning(name + ": Chưa gán BuildingPanel", this); return; }
        panel.Open(this);
    }

    private void OnDisable()
    {
        if (panel != null && panel.SelectedSlot == this) panel.Close();
    }
}
