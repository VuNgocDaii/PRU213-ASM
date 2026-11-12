using System;
using UnityEngine;

[DisallowMultipleComponent]
public class TowerController : UpgradeTarget
{
    [SerializeField] private TowerDefinition definition;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Transform firePoint;
    [SerializeField] private ProjectilePool projectilePool;
    [Header("Level I / II / III visuals")]
    [SerializeField] private Sprite[] levelSprites = new Sprite[3];
    [SerializeField] private Sprite[] levelWeaponSprites = new Sprite[3];
    [SerializeField] private SpriteRenderer weaponRenderer;
    [SerializeField] private Animator weaponAnimator;
    [Header("Special effects")]
    [SerializeField] private float aoeRadius;
    [SerializeField] private float slowMultiplier = 1;
    [SerializeField] private float slowDuration = 2.5f;

    public TowerDefinition Definition => definition;
    public int CurrentLevel { get; private set; } = 1;
    public int CurrentDamage => definition != null ? definition.GetDamage(CurrentLevel) : 0;
    public float CurrentRange => definition != null ? definition.GetRange(CurrentLevel) : 0;
    public bool IsFiringAnimation => weaponAnimator != null && weaponAnimator.isActiveAndEnabled &&
        weaponAnimator.GetCurrentAnimatorStateInfo(0).IsName("Fire" + CurrentLevel);
    public event Action<TowerController> OnShot;

    private float shootCooldownTimer;
    private IEnemyTarget currentTarget;
    private DefenseSceneContext sceneContext;
    private bool shotAnimationStarted;
    private LineRenderer rangeIndicator;
    private Material rangeMaterial;
    private float firingAnimationSpeed = 1;

    public void SetContext(DefenseSceneContext context)
    {
        sceneContext = context;
        currentTarget = null;
        StopFiringAnimation();
    }

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (projectilePool == null) projectilePool = GetComponentInChildren<ProjectilePool>();
        if (weaponAnimator == null) weaponAnimator = GetComponentInChildren<Animator>();
        if (weaponRenderer == null && weaponAnimator != null) weaponRenderer = weaponAnimator.GetComponent<SpriteRenderer>();
        if (firePoint == null) firePoint = weaponRenderer != null ? weaponRenderer.transform : transform;
        ApplyLevel(CurrentLevel);
    }

    // Preserve a level assigned by Building before Start.
    private void Start() { ApplyLevel(CurrentLevel); }

    public override void ApplyLevel(int level)
    {
        if (level < 1 || level > 3) return;
        base.ApplyLevel(level);
        CurrentLevel = level;
        if (spriteRenderer != null && levelSprites != null && levelSprites.Length >= level && levelSprites[level - 1] != null)
        {
            spriteRenderer.sprite = levelSprites[level - 1];
            if (weaponRenderer != null)
                weaponRenderer.transform.localPosition = new Vector3(0, spriteRenderer.sprite.bounds.max.y - .35f, 0);
        }
        aoeRadius = definition != null && definition.kind == TowerDefinition.Kind.Cannon
            ? (level == 1 ? 1.5f : level == 2 ? 1.8f : 2.2f) : 0;
        slowMultiplier = definition != null && definition.kind == TowerDefinition.Kind.Ice ? 1 - .1f * (level + 2) : 1;
        slowDuration = level == 1 ? 2 : level == 2 ? 2.5f : 3;
        StopFiringAnimation();
        firingAnimationSpeed = FiringAnimationSpeed();
        RefreshRangeIndicator();
    }

    private void Update()
    {
        bool canAct = definition != null && sceneContext != null && sceneContext.Game != null &&
            (sceneContext.Game.State == GameState.Preparing || sceneContext.Game.State == GameState.Fighting);
        if (weaponAnimator != null) weaponAnimator.speed = canAct ? firingAnimationSpeed : 0;
        if (!canAct || Time.deltaTime <= 0) return;
        shootCooldownTimer = Mathf.Max(0, shootCooldownTimer - Time.deltaTime);
        if (!CanTarget(currentTarget)) currentTarget = FindBestTarget();
        if (currentTarget == null)
        {
            // Let an already released shot finish its recoil animation.
            if (shotAnimationStarted && !IsFiringAnimation) StopFiringAnimation();
            return;
        }
        if (weaponRenderer != null)
        {
            Vector3 direction = currentTarget.TargetTransform.position - weaponRenderer.transform.position;
            weaponRenderer.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90);
        }
        if (shootCooldownTimer <= 0 && Shoot()) shootCooldownTimer = 1 / Mathf.Max(.01f, definition.shotsPerSecond);
    }

    private bool CanTarget(IEnemyTarget target)
    {
        if (target == null || (target is UnityEngine.Object obj && obj == null) || !target.IsAlive || target.TargetTransform == null) return false;
        return (!target.IsFlying || definition.canTargetFlying) &&
            ((Vector2)(target.TargetTransform.position - transform.position)).sqrMagnitude <= CurrentRange * CurrentRange;
    }

    private IEnemyTarget FindBestTarget()
    {
        IEnemyTarget best = null;
        float shortest = float.MaxValue;
        foreach (var enemy in sceneContext.Enemies)
        {
            if (!CanTarget(enemy)) continue;
            float score = ((Vector2)(enemy.TargetTransform.position - transform.position)).sqrMagnitude;
            if (enemy.IsFlying) score -= CurrentRange * CurrentRange + 1;
            if (score < shortest) { shortest = score; best = enemy; }
        }
        return best;
    }

    private bool Shoot()
    {
        if (projectilePool == null || !CanTarget(currentTarget)) return false;
        var bullet = projectilePool.Get();
        bullet.transform.position = MuzzlePosition();
        bullet.Launch(currentTarget, CurrentDamage, aoeRadius, slowMultiplier, slowDuration, projectilePool.Release, definition.canTargetFlying);
        if (weaponAnimator != null && weaponAnimator.HasState(0, Animator.StringToHash("Base Layer.Fire" + CurrentLevel)))
        {
            weaponAnimator.Play("Fire" + CurrentLevel, 0, 0);
            weaponAnimator.Update(0);
            shotAnimationStarted = true;
        }
        OnShot?.Invoke(this);
        return true;
    }

    private Vector3 MuzzlePosition()
    {
        if (weaponRenderer == null || firePoint != weaponRenderer.transform ||
            levelWeaponSprites == null || levelWeaponSprites.Length < CurrentLevel)
            return firePoint.position;
        var idle = levelWeaponSprites[CurrentLevel - 1];
        return idle != null
            ? weaponRenderer.transform.TransformPoint(new Vector3(idle.bounds.center.x, idle.bounds.max.y, 0))
            : firePoint.position;
    }

    private float FiringAnimationSpeed()
    {
        if (definition == null || weaponAnimator == null || weaponAnimator.runtimeAnimatorController == null) return 1;
        foreach (var clip in weaponAnimator.runtimeAnimatorController.animationClips)
            if (clip.name == definition.kind + "Fire" + CurrentLevel)
                return Mathf.Max(1, clip.length * definition.shotsPerSecond);
        return 1;
    }

    private void StopFiringAnimation()
    {
        shotAnimationStarted = false;
        if (weaponAnimator != null && weaponAnimator.runtimeAnimatorController != null &&
            weaponAnimator.HasState(0, Animator.StringToHash("Base Layer.Idle" + CurrentLevel)))
        {
            weaponAnimator.Play("Idle" + CurrentLevel, 0, 0);
            weaponAnimator.Update(0);
        }
        if (weaponRenderer != null && levelWeaponSprites != null && levelWeaponSprites.Length >= CurrentLevel)
        {
            var sprite = levelWeaponSprites[CurrentLevel - 1];
            weaponRenderer.sprite = sprite;
            if (sprite != null)
            {
                float pixelsPerUnit = spriteRenderer != null && spriteRenderer.sprite != null ? spriteRenderer.sprite.pixelsPerUnit : 50;
                weaponRenderer.transform.localScale = Vector3.one * (sprite.pixelsPerUnit / pixelsPerUnit);
            }
        }
    }

    public void SetRangeVisible(bool visible)
    {
        if (visible && rangeIndicator == null)
        {
            var go = new GameObject("AttackRange");
            go.transform.SetParent(transform, false);
            rangeIndicator = go.AddComponent<LineRenderer>();
            rangeMaterial = new Material(Shader.Find("Sprites/Default"));
            rangeIndicator.sharedMaterial = rangeMaterial;
            rangeIndicator.useWorldSpace = true;
            rangeIndicator.loop = true;
            rangeIndicator.widthMultiplier = .04f;
            rangeIndicator.positionCount = 96;
            rangeIndicator.startColor = rangeIndicator.endColor = new Color(.4f, 1, .7f, .8f);
            rangeIndicator.sortingOrder = 1500;
        }
        if (rangeIndicator != null) { rangeIndicator.enabled = visible; RefreshRangeIndicator(); }
    }

    private void RefreshRangeIndicator()
    {
        if (rangeIndicator == null || !rangeIndicator.enabled) return;
        for (int i = 0; i < rangeIndicator.positionCount; i++)
        {
            float angle = i * Mathf.PI * 2 / rangeIndicator.positionCount;
            rangeIndicator.SetPosition(i, transform.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * CurrentRange);
        }
    }

    private void OnDestroy() { if (rangeMaterial != null) Destroy(rangeMaterial); }
    private void OnDrawGizmosSelected() { Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, CurrentRange); }
}
