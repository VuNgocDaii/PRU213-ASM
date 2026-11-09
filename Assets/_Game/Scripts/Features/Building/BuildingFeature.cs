// Person 2 owns placement UI/catalog. All construction payments go through DefenseBuildingSlot.TryBuild.
public class BuildingFeature : DefenseModule
{
    // Use Context.Slots and Context.Progress; consume prefabs through Inspector/catalog entries.
    // Tower UI requests construction through the same slot, without subtracting Credits again.
}
