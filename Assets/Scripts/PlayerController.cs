using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float jumpForce = 5f; // 점프 높이 설정

    [Header("Ground Detection")]
    public Transform groundCheck; // 발밑 위치를 표시할 오브젝트
    public float groundCheckRadius = 0.2f; // 감지 반경
    public LayerMask groundLayer; // 땅으로 인식할 레이어

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim;
    private bool isGrounded; // 현재 땅에 있는지 여부

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        // 1. 지면 감지 (발밑에 원을 그려서 땅 레이어와 닿았는지 확인)
        // groundCheck가 비어있다면 오류가 나므로 꼭 할당해야 합니다.
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }

        // 2. 입력 받기
        float moveInput = Input.GetAxisRaw("Horizontal");

        // 3. 점프 (스페이스바 & 땅에 있을 때만)
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            anim.SetTrigger("DoJump"); // 점프 애니메이션 발동
        }

        // 4. 물리 이동 (좌우 이동은 기존 속도 유지, 상하 속도는 리지드바디 유지)
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        // 5. 방향 뒤집기
        if (moveInput > 0) sr.flipX = false;
        else if (moveInput < 0) sr.flipX = true;

        // 6. 애니메이션 업데이트
        anim.SetFloat("Speed", Mathf.Abs(moveInput));
        anim.SetBool("IsGrounded", isGrounded); // 공중/지상 상태 전달
    }

    // 에디터에서 지면 감지 범위를 눈으로 보기 위한 함수 (게임엔 영향 없음)
    void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}