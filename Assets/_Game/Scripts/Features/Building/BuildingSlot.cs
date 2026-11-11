using UnityEngine;

[DisallowMultipleComponent]
public class BuildingSlot : Interactable
{
    [SerializeField] private BuildingController building;
    [SerializeField] private BuildingUpgradeController upgrades;
    [SerializeField] private BuildingPanel panel;
    [SerializeField] private Transform player;
    [SerializeField, Min(0.1f)] private float interactionDistance = 1.5f;

    [Header("Defense Slot Binding")]
    [SerializeField] private DefenseBuildingSlot defenseSlot;

    public BuildingController Building => building;
    public BuildingUpgradeController Upgrades => upgrades;
    public DefenseBuildingSlot DefenseSlot => defenseSlot;
    public BuildingPanel Panel { get => panel; set => panel = value; }
    public Transform Player { get => player; set => player = value; }

    public bool IsPlayerInRange => player != null &&
        ((Vector2)(player.position - transform.position)).sqrMagnitude <= interactionDistance * interactionDistance;

    public void BindDefenseSlot(DefenseBuildingSlot slot, DefenseSceneContext context, BuildingPanel sharedPanel = null, Transform playerTransform = null)
    {
        defenseSlot = slot;
        if (sharedPanel != null) panel = sharedPanel;
        if (playerTransform != null) player = playerTransform;

        // Auto-resolve sibling components when freshly AddComponent'd
        if (building == null) building = GetComponent<BuildingController>();
        if (upgrades == null) upgrades = GetComponent<BuildingUpgradeController>();

        if (building != null && slot != null)
        {
            building.BindDefenseSlot(slot, context != null ? context.Progress : null);
        }

        if (upgrades != null && building != null)
        {
            bool isTower = slot != null && slot.SlotKind == DefenseBuildingSlot.Kind.Tower;
            upgrades.SetCastleProgress(context != null ? context.Progress : null, isTower);
        }
    }

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
