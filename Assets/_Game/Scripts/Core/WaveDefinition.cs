using UnityEngine;

[CreateAssetMenu(menuName = "PolishCow/Wave Definition")]
public class WaveDefinition : ScriptableObject
{
    [System.Serializable]
    public struct Group { public EnemyDefinition enemy; [Min(1)] public int count; [Min(0.01f)] public float spawnInterval; }
    [Min(0)] public float preparationSeconds = 90;
    [Min(1)] public float healthMultiplier = 1;
    [Min(0)] public int completionCredits = 50;
    public Group[] groups;
}
