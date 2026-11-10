using System;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Cấu hình di chuyển")]
    [SerializeField] private float speed = 12f;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Hoạt ảnh Nổ / Va chạm")]
    [SerializeField] private Animator animator;
    [SerializeField] private string flyAnimState = "";       // Tên state lúc bay (ví dụ: "Cannon")
    [SerializeField] private string boomAnimState = "";      // Tên state lúc nổ (ví dụ: "Cannon_Boom")
    [SerializeField] private float boomDuration = 0.4f;      // Thời gian nổ trước khi biến mất (0.4 giây)
    [SerializeField] private float rotationOffset = -90f;    // Bù góc lệch cho sprite vẽ dọc

    private IEnemyTarget target;
    private Vector3 lastTargetPos;
    private int damage;
    private float aoeRadius;
    private float slowMultiplier;
    private float slowDuration;
    private Action<Projectile> returnToPool;
    private bool isExploding = false;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        // Đảm bảo đạn luôn hiển thị nổi trên mặt cỏ
        if (spriteRenderer != null) spriteRenderer.sortingOrder = 100;
    }

    public void Launch(IEnemyTarget enemyTarget, int dmg, float aoe, float slowMult, float slowTime, Action<Projectile> onHit)
    {
        target = enemyTarget;
        lastTargetPos = (enemyTarget != null && enemyTarget.TargetTransform != null)
            ? enemyTarget.TargetTransform.position
            : transform.position;
        damage = dmg;
        aoeRadius = aoe;
        slowMultiplier = slowMult;
        slowDuration = slowTime;
        returnToPool = onHit;
        isExploding = false;

        // Bật lại animation bay nếu có
        if (animator != null && !string.IsNullOrEmpty(flyAnimState))
        {
            animator.Play(flyAnimState, 0, 0f);
        }

        gameObject.SetActive(true);
    }

    private void Update()
    {
        // Nếu đạn đang nổ thì đứng yên, không bay tiếp
        if (isExploding) return;

        if (target != null && target.IsAlive && target.TargetTransform != null)
        {
            lastTargetPos = target.TargetTransform.position;
        }

        Vector3 direction = (lastTargetPos - transform.position).normalized;
        float moveDistance = speed * Time.deltaTime;

        // Xoay đầu đạn theo hướng bay (cộng thêm góc bù)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + rotationOffset;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // Kiểm tra xem đã chạm đích chưa
        if (Vector3.Distance(transform.position, lastTargetPos) <= moveDistance)
        {
            transform.position = lastTargetPos;
            OnHitTarget();
        }
        else
        {
            transform.position += direction * moveDistance;
        }
    }

    private void OnHitTarget()
    {
        isExploding = true;

        // 1. Gây sát thương
        if (aoeRadius > 0f)
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, aoeRadius);
            foreach (var col in colliders)
            {
                if (col.TryGetComponent<IDamageable>(out var dmgTarget))
                {
                    dmgTarget.TakeDamage(damage);
                }
            }
        }
        else
        {
            if (target != null && target.IsAlive)
            {
                target.TakeDamage(damage);

                if (slowMultiplier < 1f && target is ISlowable slowable)
                {
                    slowable.ApplySlow(slowMultiplier, slowDuration);
                }
            }
        }

        // 2. Kích hoạt Animation Nổ nếu có
        if (animator != null && !string.IsNullOrEmpty(boomAnimState))
        {
            animator.Play(boomAnimState, 0, 0f);
            Invoke(nameof(Recycle), boomDuration); // Chờ nổ xong 0.4s rồi mới thu hồi
        }
        else
        {
            Recycle();
        }
    }

    private void Recycle()
    {
        returnToPool?.Invoke(this);
    }
}