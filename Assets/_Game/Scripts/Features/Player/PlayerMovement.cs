using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 4f;
    [SerializeField] private Rigidbody2D playerRigidbody;
    [SerializeField] private SpriteRenderer playerSpriteRenderer;
    [SerializeField] private Sprite upSprite;
    [SerializeField] private Sprite downSprite;
    [SerializeField] private Sprite leftSprite;
    [SerializeField] private Sprite rightSprite;

    private Vector2 moveInput;

    private void Awake()
    {
        // Dùng Rigidbody2D trên chính Player nếu chưa kéo thả tham chiếu.
        if (playerRigidbody == null)
        {
            playerRigidbody = GetComponent<Rigidbody2D>();
        }
        if (playerSpriteRenderer == null)
        {
            playerSpriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void Update()
    {
        moveInput = Vector2.zero;
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        // Hai phím đối nhau triệt tiêu; WASD và mũi tên dùng chung.
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            moveInput.x -= 1f;
        }
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            moveInput.x += 1f;
        }
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
        {
            moveInput.y -= 1f;
        }
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
        {
            moveInput.y += 1f;
        }

        // Giữ tốc độ đi chéo bằng tốc độ đi thẳng.
        if (moveInput.sqrMagnitude > 1f)
        {
            moveInput = moveInput.normalized;
        }

        UpdateFacingDirection();
    }

    private void UpdateFacingDirection()
    {
        // Khi đứng yên, giữ nguyên sprite của hướng nhìn gần nhất.
        if (moveInput == Vector2.zero || playerSpriteRenderer == null)
        {
            return;
        }

        Sprite facingSprite;
        // Nếu hai trục bằng nhau thì ưu tiên chiều ngang.
        if (Mathf.Abs(moveInput.x) >= Mathf.Abs(moveInput.y))
        {
            if (moveInput.x > 0f)
            {
                facingSprite = rightSprite;
            }
            else
            {
                facingSprite = leftSprite;
            }
        }
        else
        {
            if (moveInput.y > 0f)
            {
                facingSprite = upSprite;
            }
            else
            {
                facingSprite = downSprite;
            }
        }

        if (facingSprite != null)
        {
            playerSpriteRenderer.sprite = facingSprite;
        }
    }

    private void FixedUpdate()
    {
        if (playerRigidbody != null)
        {
            // Unity 6: vận tốc tính bằng unit/giây, không nhân deltaTime.
            playerRigidbody.linearVelocity = moveInput * moveSpeed;
        }
    }

    private void OnDisable()
    {
        // Dừng hẳn khi tắt component hoặc Player.
        moveInput = Vector2.zero;
        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector2.zero;
        }
    }
}
