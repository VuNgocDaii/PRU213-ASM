using UnityEngine;

[DisallowMultipleComponent]
public class TowerController : UpgradeTarget
{
    [Header("Cấu hình & Data")]
    [SerializeField] private TowerDefinition definition; // Gán Tower0/1/2/3.asset
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Transform firePoint;
    [SerializeField] private ProjectilePool projectilePool;

    [Header("Sprites theo 3 cấp")]
    [SerializeField] private Sprite[] levelSprites = new Sprite[3];

    [Header("Hiệu ứng đặc biệt")]
    [SerializeField] private float aoeRadius = 0f;        // Pháo: 1.5 ô
    [SerializeField] private float slowMultiplier = 1f;   // Băng: C1=0.7, C2=0.6, C3=0.5
    [SerializeField] private float slowDuration = 2.5f;   // Băng: 2.5s

    public TowerDefinition Definition => definition;
    public int CurrentLevel { get; private set; } = 1;

    private float shootCooldownTimer = 0f;
    private IEnemyTarget currentTarget;
    private DefenseSceneContext sceneContext;

    // Được gọi khi slot sinh tháp
    public void SetContext(DefenseSceneContext context)
    {
        sceneContext = context;
    }

    private void Awake()
    {
        // Tự động tìm ProjectilePool ở object con nếu trong Inspector quên kéo thả
        if (projectilePool == null)
        {
            projectilePool = GetComponentInChildren<ProjectilePool>();
        }

        // Tự động gán firePoint nếu quên kéo
        if (firePoint == null)
        {
            firePoint = transform;
        }
    }

    private void Start()
    {
        ApplyLevel(1);
    }

    // Tuân thủ hợp đồng nhóm: ApplyLevel thiết lập cấp tuyệt đối 1..3
    public override void ApplyLevel(int level)
    {
        base.ApplyLevel(level);
        if (level < 1 || level > 3) return;

        CurrentLevel = level;

        // Cập nhật Sprite theo cấp độ nếu có
        if (spriteRenderer != null && levelSprites != null && level - 1 < levelSprites.Length && levelSprites[level - 1] != null)
        {
            spriteRenderer.sprite = levelSprites[level - 1];
        }

        // Cập nhật chỉ số theo cấp từ bảng cân bằng
        if (definition.kind == TowerDefinition.Kind.Ice)
        {
            // Băng: Làm chậm 30% / 40% / 50% tương ứng multiplier = 0.7, 0.6, 0.5
            slowMultiplier = level == 1 ? 0.7f : level == 2 ? 0.6f : 0.5f;
            slowDuration = level == 1 ? 2.0f : level == 2 ? 2.5f : 3.0f;
        }
        else if (definition.kind == TowerDefinition.Kind.Cannon)
        {
            aoeRadius = level == 1 ? 1.5f : level == 2 ? 1.8f : 2.2f;
        }
    }

    private void Update()
    {
        shootCooldownTimer -= Time.deltaTime;

        // Kiểm tra mục tiêu hiện tại còn hợp lệ không
        if (currentTarget == null || !currentTarget.IsAlive || currentTarget.TargetTransform == null ||
            Vector2.Distance(transform.position, currentTarget.TargetTransform.position) > definition.range)
        {
            currentTarget = FindBestTarget();
        }

        // Bắn khi đã hồi chiêu và có mục tiêu
        if (currentTarget != null && shootCooldownTimer <= 0f)
        {
            Shoot();
            shootCooldownTimer = 1f / definition.shotsPerSecond;
        }
    }

    private IEnemyTarget FindBestTarget()
    {
        if (sceneContext == null || sceneContext.Enemies == null) return null;

        IEnemyTarget best = null;
        float shortestDist = float.MaxValue;

        var enemies = sceneContext.Enemies;
        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            if (e == null || !e.IsAlive || e.TargetTransform == null) continue;

            // Nếu tháp không bắn được mục tiêu bay (Ruồi) mà quái đang bay thì bỏ qua
            if (e.IsFlying && !definition.canTargetFlying) continue;

            float dist = Vector2.Distance(transform.position, e.TargetTransform.position);
            if (dist <= definition.range)
            {
                // ƯU TIÊN ĐẶC BIỆT: Nếu là Ruồi (quái bay) -> ưu tiên bắn trước để bảo vệ bò!
                if (e.IsFlying)
                {
                    return e;
                }

                if (dist < shortestDist)
                {
                    shortestDist = dist;
                    best = e;
                }
            }
        }

        return best;
    }

    private void Shoot()
    {
        if (projectilePool == null || currentTarget == null) return;

        Projectile bullet = projectilePool.Get();
        bullet.transform.position = firePoint != null ? firePoint.position : transform.position;

        int currentDamage = (definition.damage != null && CurrentLevel - 1 < definition.damage.Length)
            ? definition.damage[CurrentLevel - 1]
            : 10;

        bullet.Launch(currentTarget, currentDamage, aoeRadius, slowMultiplier, slowDuration, projectilePool.Release);
        Debug.Log($"[{name}] Đang bắn đạn vào: {currentTarget.TargetTransform.name}");
    }

    private void OnDrawGizmosSelected()
    {
        if (definition != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, definition.range);
        }
    }
}