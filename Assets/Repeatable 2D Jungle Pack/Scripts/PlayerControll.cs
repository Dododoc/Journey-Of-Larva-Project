using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("설정 값")]
    public float moveSpeed = 5f;    // 이동 속도
    public float jumpForce = 10f;   // 점프 힘

    [Header("점프 판정")]
    public Transform groundCheck;   // 발바닥 위치 (빈 오브젝트)
    public float checkRadius = 0.3f; // 바닥 감지 범위
    public LayerMask groundLayer;   // 땅으로 인식할 레이어 (Ground)

    private Rigidbody2D rb;
    private bool isGrounded;        // 땅에 닿아있는지 확인
    private float moveInput;        // 좌우 입력 값

    void Start()
    {
        rb = GetComponent<Rigidbody2D>(); // 리지드바디 가져오기
    }

    void Update() // 입력 및 방향 전환
    {
        // 1. 좌우 입력 받기
        moveInput = Input.GetAxisRaw("Horizontal");

        // 2. 점프 입력 (스페이스바) & 땅에 있을 때만
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // 3. 캐릭터 방향 뒤집기 (수정됨: 현재 크기 유지!)
        // 캐릭터의 현재 크기(Scale)를 가져와서 부호만 바꿉니다.
        if (moveInput > 0) // 오른쪽 이동
        {
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        else if (moveInput < 0) // 왼쪽 이동
        {
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
    }

    void FixedUpdate() // 물리 이동
    {
        // 4. 바닥 감지
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);

        // 5. 이동 적용
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }
}