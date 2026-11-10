using System;
using UnityEngine;

// Phần chung của mọi quái: máu + thanh máu, trúng đòn, làm chậm, chết, hoạt ảnh.
// Lớp con (GroundEnemy, FlyEnemy) chỉ viết cách di chuyển trong Move().
public abstract class EnemyController : DefenseModule, IEnemyTarget, ISlowable
{
    [SerializeField] protected EnemyDefinition definition;   // Data/Defense/Enemy0..4
    [SerializeField] protected SpriteRenderer body;
    [SerializeField] private Sprite[] moveFrames;             // hoạt ảnh đi (lặp lại)
    [SerializeField] private Sprite[] hitFrames;              // hoạt ảnh trúng đòn (chạy 1 lần)
    [SerializeField] private Sprite[] dieFrames;              // hoạt ảnh chết (chạy xong thì biến mất)
    [SerializeField] private float framesPerSecond = 10f;
    [SerializeField] private float reachDistance = 0.2f;
    [SerializeField] private GameObject healthBar;
    [SerializeField] private Transform healthFill;            // scale X = máu còn / máu tối đa

    public event Action<EnemyController> OnDespawned;   // WaveSpawner đếm quái và cất vào pool

    private int health;
    private int maxHealth;
    private bool isAlive;
    private float slowMultiplier = 1f;
    private float slowTimer;
    private Sprite[] currentFrames;   // hoạt ảnh đang chạy
    private float animTime;           // hoạt ảnh đó đã chạy bao lâu

    public EnemyDefinition Definition => definition;
    public Transform TargetTransform => transform;
    public bool IsAlive => isAlive;
    public abstract bool IsFlying { get; }

    protected abstract void Move();
    protected virtual void OnSpawned() { }

    // Spawner gọi mỗi lần thả quái (kể cả lấy lại từ pool)
    public void Spawn(DefenseSceneContext context, Vector3 position, float healthMultiplier)
    {
        transform.position = position;
        maxHealth = Mathf.Max(1, Mathf.RoundToInt(definition.health * healthMultiplier));
        health = maxHealth;
        isAlive = true;
        slowMultiplier = 1f;
        slowTimer = 0f;
        healthBar.SetActive(true);
        healthFill.localScale = Vector3.one;
        Play(moveFrames);
        OnSpawned();
        Initialize(context);
        gameObject.SetActive(true);   // bật lên -> OnConnected -> có trong danh sách quái
    }

    protected override void OnConnected() { Context.RegisterEnemy(this); }
    protected override void OnDisconnected() { Context.UnregisterEnemy(this); }

    private void Update()
    {
        GameState state = Context != null ? Context.Game.State : GameState.Paused;
        if (state == GameState.Paused || state == GameState.Won || state == GameState.Lost) return;

        // Hoạt ảnh: chạy hết thì -> đang chết: biến mất; còn sống: quay lại hoạt ảnh đi
        animTime += Time.deltaTime;
        if (animTime * framesPerSecond >= currentFrames.Length)
        {
            if (!isAlive)
            {
                Despawn();
                return;
            }
            Play(moveFrames);
        }
        if (currentFrames.Length > 0) body.sprite = currentFrames[(int)(animTime * framesPerSecond)];

        if (!isAlive) return;   // đang chết thì đứng yên

        if (slowTimer > 0f)
        {
            slowTimer -= Time.deltaTime;
            if (slowTimer <= 0f) slowMultiplier = 1f;   // hết slow thì về tốc độ gốc
        }
        Move();
    }

    private void Play(Sprite[] frames)
    {
        currentFrames = frames;
        animTime = 0f;
    }

    // Đi về phía destination, trả về true khi đã tới nơi
    protected bool MoveTowards(Vector3 destination)
    {
        float step = definition.speed * slowMultiplier * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, destination, step);
        body.flipX = destination.x < transform.position.x;
        return Vector3.Distance(transform.position, destination) <= reachDistance;
    }

    // Tới thành: trừ máu thành rồi biến mất ngay, không có thưởng
    protected void ReachCastle()
    {
        if (definition.castleDamage > 0) Context.CastleHealth.TakeDamage(definition.castleDamage);
        Despawn();
    }

    public void TakeDamage(int amount)
    {
        if (!isAlive || amount <= 0) return;
        health -= amount;
        healthFill.localScale = new Vector3(Mathf.Max(0f, (float)health / maxHealth), 1f, 1f);

        if (health > 0)
        {
            Play(hitFrames);
            return;
        }

        // Chết: rời danh sách quái ngay (tháp thôi nhắm), báo Core đúng 1 lần (Core cộng tiền/EXP,
        // boss chết ở wave cuối thì thắng), rồi chạy hoạt ảnh chết.
        isAlive = false;
        Context.UnregisterEnemy(this);
        healthBar.SetActive(false);
        bool isBoss = definition.kind == EnemyDefinition.Kind.Boss;
        Context.Game.ReportEnemyKilled(definition.experience, definition.credits, isBoss);
        Play(dieFrames);
    }

    // Slow mạnh nhất thắng, thời gian lấy cái dài hơn -> không nhân dồn
    public void ApplySlow(float speedMultiplier, float seconds)
    {
        if (!isAlive || seconds <= 0f) return;
        slowMultiplier = Mathf.Min(slowMultiplier, Mathf.Clamp(speedMultiplier, 0.1f, 1f));
        slowTimer = Mathf.Max(slowTimer, seconds);
    }

    protected void Despawn()
    {
        isAlive = false;
        gameObject.SetActive(false);   // tắt -> OnDisconnected -> rời danh sách quái
        OnDespawned?.Invoke(this);
    }
}
