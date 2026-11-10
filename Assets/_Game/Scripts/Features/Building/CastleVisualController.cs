using System;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class CastleVisualController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CastleProgress progress;
    [SerializeField] private CastleHealth health;

    [Header("Visual Stages (Level 1, 2, 3)")]
    [SerializeField] private GameObject[] stageGameObjects = new GameObject[3];
    [SerializeField] private SpriteRenderer mainRenderer;
    [SerializeField] private Sprite[] stageSprites = new Sprite[3];
    [SerializeField] private GameObject destroyedVisual;

    [Header("VFX Hooks & Effects")]
    [SerializeField] private Transform vfxHookPoint;
    [SerializeField] private ParticleSystem levelUpVFX;
    [SerializeField] private ParticleSystem destructionVFX;
    [SerializeField] private UnityEvent<int> onLevelUp;
    [SerializeField] private UnityEvent onDestroyed;

    private int currentVisualLevel = 1;
    private bool isDestroyed;

    public int CurrentVisualLevel => currentVisualLevel;
    public bool IsDestroyed => isDestroyed;
    public Transform VFXHookPoint => vfxHookPoint != null ? vfxHookPoint : transform;

    private void Awake()
    {
        if (progress != null)
        {
            progress.OnCastleLevelUp += OnCastleLevelUp;
            currentVisualLevel = progress.CurrentLevel;
        }
        if (health != null)
        {
            health.OnCastleDestroyed += OnCastleDestroyed;
        }
        UpdateVisualStage(currentVisualLevel, playVFX: false);
    }

    public void Initialize(CastleProgress castleProgress, CastleHealth castleHealth)
    {
        if (progress != null) progress.OnCastleLevelUp -= OnCastleLevelUp;
        if (health != null) health.OnCastleDestroyed -= OnCastleDestroyed;

        progress = castleProgress;
        health = castleHealth;

        if (progress != null)
        {
            progress.OnCastleLevelUp += OnCastleLevelUp;
            currentVisualLevel = progress.CurrentLevel;
        }
        if (health != null)
        {
            health.OnCastleDestroyed += OnCastleDestroyed;
        }

        UpdateVisualStage(currentVisualLevel, playVFX: false);
    }

    private void OnDestroy()
    {
        if (progress != null) progress.OnCastleLevelUp -= OnCastleLevelUp;
        if (health != null) health.OnCastleDestroyed -= OnCastleDestroyed;
    }

    private void OnCastleLevelUp(int level)
    {
        ApplyCastleLevel(level);
    }

    private void OnCastleDestroyed()
    {
        TriggerDestruction();
    }

    public void ApplyCastleLevel(int level)
    {
        if (level < 1 || level > 3) return;
        if (currentVisualLevel == level)
        {
            // Idempotent: repeated calls do not duplicate VFX or accumulate state
            return;
        }

        currentVisualLevel = level;
        UpdateVisualStage(currentVisualLevel, playVFX: true);
        onLevelUp?.Invoke(currentVisualLevel);
    }

    public void TriggerDestruction()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        if (destructionVFX != null)
        {
            destructionVFX.Play();
        }

        if (destroyedVisual != null)
        {
            destroyedVisual.SetActive(true);
            for (int i = 0; i < stageGameObjects.Length; i++)
            {
                if (stageGameObjects[i] != null) stageGameObjects[i].SetActive(false);
            }
        }

        onDestroyed?.Invoke();
    }

    private void UpdateVisualStage(int level, bool playVFX)
    {
        int index = Mathf.Clamp(level - 1, 0, 2);

        // Update child GameObjects if assigned
        for (int i = 0; i < stageGameObjects.Length; i++)
        {
            if (stageGameObjects[i] != null)
            {
                stageGameObjects[i].SetActive(i == index && !isDestroyed);
            }
        }

        // Update sprite on main renderer if assigned
        if (mainRenderer != null && stageSprites != null && index < stageSprites.Length && stageSprites[index] != null)
        {
            mainRenderer.sprite = stageSprites[index];
        }

        if (destroyedVisual != null)
        {
            destroyedVisual.SetActive(isDestroyed);
        }

        if (playVFX && levelUpVFX != null)
        {
            levelUpVFX.Play();
        }
    }
}
