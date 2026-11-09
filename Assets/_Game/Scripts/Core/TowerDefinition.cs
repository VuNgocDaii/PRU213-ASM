using UnityEngine;

[CreateAssetMenu(menuName = "PolishCow/Tower Definition")]
public class TowerDefinition : ScriptableObject
{
    public enum Kind { Arrow, Cannon, Ice, Sniper }
    public Kind kind;
    public GameObject prefab;
    [Range(1, 3)] public int unlockCastleLevel = 1;
    [Min(1)] public int buildPrice = 80;
    public int[] upgradePrices = { 60, 120 };
    public int[] damage = { 8, 12, 18 };
    [Min(0.01f)] public float shotsPerSecond = 1.2f, range = 4;
    public bool canTargetFlying = true;
}
