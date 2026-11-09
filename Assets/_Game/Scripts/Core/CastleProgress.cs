using System;
using UnityEngine;

[DisallowMultipleComponent]
public class CastleProgress : MonoBehaviour
{
    [SerializeField] private CastleDefinition definition;
    public CastleDefinition Definition => definition;
    public int Experience { get; private set; }
    public int CurrentLevel { get; private set; } = 1;
    public event Action<int> OnExpChanged;
    public event Action<int> OnCastleLevelUp;

    public void AddExperience(int amount)
    {
        if (amount <= 0 || definition == null || definition.MaxLevel == 0) return;
        Experience = (int)Math.Min(int.MaxValue, (long)Experience + amount);
        while (CurrentLevel < definition.MaxLevel && Experience >= definition.GetLevel(CurrentLevel + 1).requiredExp)
        {
            CurrentLevel++;
            OnCastleLevelUp?.Invoke(CurrentLevel);
        }
        OnExpChanged?.Invoke(Experience);
    }

    [ContextMenu("Foundation Test/Add 495 EXP")]
    private void TestLevel2() { if (Application.isPlaying) AddExperience(495); }
    [ContextMenu("Foundation Test/Add 3210 EXP")]
    private void TestLevel3() { if (Application.isPlaying) AddExperience(3210); }
}
