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
    [Tooltip("World-space radius for levels I, II and III, measured from the building slot.")]
    public float[] rangeByLevel = new float[0];
    public bool canTargetFlying = true;

    public int GetDamage(int level) => damage != null && damage.Length > 0
        ? Mathf.Max(1, damage[Mathf.Clamp(level - 1, 0, damage.Length - 1)]) : 1;

    public float GetRange(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, 2);
        return rangeByLevel != null && index < rangeByLevel.Length && rangeByLevel[index] > 0
            ? rangeByLevel[index] : Mathf.Max(.1f, range) * (1 + .2f * index);
    }

    public static string LevelLabel(int level) => level >= 3 ? "III" : level == 2 ? "II" : "I";
}
