using UnityEngine;

[CreateAssetMenu(menuName = "PolishCow/Enemy Definition")]
public class EnemyDefinition : ScriptableObject
{
    public enum Kind { Normal, Fast, Tank, Fly, Boss }
    public Kind kind;
    public GameObject prefab;
    [Min(1)] public int health = 30;
    [Min(0.01f)] public float speed = 1;
    [Min(0)] public int castleDamage = 5, credits = 5, experience = 10;
}
