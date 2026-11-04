using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class CowMovement : MonoBehaviour
{
    [Header("Tham chiếu")]
    [SerializeField] private RoamingArea roamingArea;
    [SerializeField] private Rigidbody2D cowRigidbody;
    [SerializeField] private SpriteRenderer cowSpriteRenderer;

    [Header("Cấu hình di chuyển")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 1f;
    [SerializeField] private Vector2 padding = new Vector2(0.35f, 0.4f);
    [SerializeField, Min(0.5f)] private float minWaitTime = 2f;
    [SerializeField, Min(0.5f)] private float maxWaitTime = 5f;
    [SerializeField, Min(0.01f)] private float reachThreshold = 0.05f;

    private Vector2 currentDestination;
    private bool isWaiting;
    private float waitTimer;

    public void SetRoamingArea(RoamingArea area)
    {
        roamingArea = area;
    }

    private void Awake()
    {
        if (cowRigidbody == null)
        {
            cowRigidbody = GetComponent<Rigidbody2D>();
        }
        if (cowSpriteRenderer == null)
        {
            cowSpriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void Start()
    {
        PickNewDestination();
    }

    private void FixedUpdate()
    {
        if (roamingArea == null || cowRigidbody == null)
        {
            return;
        }

        if (isWaiting)
        {
            waitTimer -= Time.fixedDeltaTime;
            if (waitTimer <= 0f)
            {
                isWaiting = false;
                PickNewDestination();
            }
            return;
        }

        Vector2 currentPosition = cowRigidbody.position;
        float distanceToDest = Vector2.Distance(currentPosition, currentDestination);

        if (distanceToDest <= reachThreshold)
        {
            // Đã đến điểm đích, chuyển sang trạng thái nghỉ ngơi
            isWaiting = true;
            waitTimer = Random.Range(minWaitTime, maxWaitTime);
            return;
        }

        // Tính bước di chuyển tiếp theo
        Vector2 nextPosition = Vector2.MoveTowards(
            currentPosition,
            currentDestination,
            moveSpeed * Time.fixedDeltaTime);

        // Kẹp toạ độ theo RoamingArea để đảm bảo không bao giờ vượt biên
        Vector2 clampedPosition = roamingArea.ClampPosition(nextPosition, padding);

        // Cập nhật hướng mặt theo chuyển động ngang
        float deltaX = clampedPosition.x - currentPosition.x;
        if (Mathf.Abs(deltaX) > 0.001f && cowSpriteRenderer != null)
        {
            // sprite gốc nhìn sang trái: khi đi sang phải thì flipX = true
            cowSpriteRenderer.flipX = (deltaX > 0f);
        }

        // Di chuyển bằng Rigidbody2D.MovePosition theo quy chuẩn
        cowRigidbody.MovePosition(clampedPosition);

        // Nếu vị trí bị kẹt ở biên và không thể di chuyển tiếp đến đích
        if (Vector2.Distance(clampedPosition, currentPosition) < 0.0001f && distanceToDest > reachThreshold)
        {
            // Chọn điểm đích khác để không bị đứng kẹt
            PickNewDestination();
        }
    }

    private void PickNewDestination()
    {
        if (roamingArea == null)
        {
            return;
        }

        currentDestination = roamingArea.GetRandomPoint(padding);
    }

    private void OnDisable()
    {
        isWaiting = false;
    }
}
