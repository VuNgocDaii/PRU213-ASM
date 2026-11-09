using UnityEngine;

[CreateAssetMenu(menuName = "PolishCow/Castle Definition")]
public class CastleDefinition : ScriptableObject
{
    [System.Serializable]
    public struct Level
    {
        public int requiredExp, maxHealth, towerSlots, cowSlots, machineSlots;
    }

    public Level[] levels = {
        new Level { requiredExp = 0, maxHealth = 120, towerSlots = 4, cowSlots = 3, machineSlots = 1 },
        new Level { requiredExp = 495, maxHealth = 200, towerSlots = 8, cowSlots = 5, machineSlots = 2 },
        new Level { requiredExp = 3210, maxHealth = 320, towerSlots = 12, cowSlots = 8, machineSlots = 4 }
    };

    public int MaxLevel => Mathf.Min(3, levels == null ? 0 : levels.Length);
    public Level GetLevel(int level) => levels[Mathf.Clamp(level, 1, MaxLevel) - 1];
}
