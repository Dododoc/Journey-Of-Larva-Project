using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;

    [Header("Attack Settings (Dash)")]
    public float dashSpeed = 15f;     // 돌격 속도
    public float dashDuration = 0.2f; // 돌격 지속 시간
    public float dashCooldown = 1f;   // 쿨타임
    private bool isDashing = false;   // 현재 돌격 중인가?
    private bool canDash = true;      // 쿨타임이 끝났는가?

    [Header("Ground Check (BoxCast)")]
    public Vector2 boxSize = new Vector2(0.8f, 0.2f);
    public float castDistance = 0.2f;
    public LayerMask groundLayer; 

    // 내부 변수들
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim;

    private bool isGrounded;
    private Vector2 surfaceNormal;
    private float jumpCooldown;
    private bool isKnockedBack;
    private float defaultGravity;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        defaultGravity = rb.gravityScale;
    }

    void Update()
    {
        if (jumpCooldown > 0) jumpCooldown -= Time.deltaTime;

        CheckGround();
        ProcessInput();
        UpdateAnimation();
    }

    void CheckGround()
    {
        if (jumpCooldown > 0)
        {
            isGrounded = false;
            surfaceNormal = Vector2.up;
            return;
        }

        Vector2 boxOrigin = (Vector2)transform.position + Vector2.up * 0.3f;
        RaycastHit2D hit = Physics2D.BoxCast(boxOrigin, boxSize, 0f, Vector2.down, castDistance + 0.3f, groundLayer);
        
        isGrounded = hit.collider != null;

        if (isGrounded)
        {
            surfaceNormal = hit.normal;
        }
        else
        {
            surfaceNormal = Vector2.up;
        }

        transform.rotation = Quaternion.identity; 
    }

    void ProcessInput()
    {
        // 넉백 중이거나 돌격 중일 때는 이동 입력을 받지 않음 (돌격 궤도 유지)
        if (isKnockedBack || isDashing) return;

        // [공격] Z키 입력 처리
        if (Input.GetKeyDown(KeyCode.Z) && canDash)
        {
            StartCoroutine(DashRoutine());
            return; // 돌격 시작하면 아래 이동 로직 무시
        }

        float moveInput = Input.GetAxisRaw("Horizontal");

        // [점프]
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            jumpCooldown = 0.2f;
            isGrounded = false;
            rb.gravityScale = defaultGravity;
            
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            anim.SetTrigger("DoJump");
            return;
        }

        // [이동]
        if (isGrounded && moveInput != 0)
        {
            rb.gravityScale = defaultGravity;

            Vector2 slopeDir = Vector2.Perpendicular(surfaceNormal).normalized;
            Vector2 moveDir = slopeDir * -moveInput;

            rb.linearVelocity = moveDir * moveSpeed;

            // 경사면 접착력 유지
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y - 5f); 
        }
        else if (!isGrounded)
        {
            rb.gravityScale = defaultGravity;
            rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
        }
        else 
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f; 
        }

        // [방향 반전]
        if (moveInput > 0) sr.flipX = false;
        else if (moveInput < 0) sr.flipX = true;
    }

    void UpdateAnimation()
    {
        anim.SetFloat("Speed", rb.linearVelocity.magnitude > 0.1f ? rb.linearVelocity.magnitude : 0f);
        anim.SetBool("IsGrounded", isGrounded);
        anim.SetFloat("VerticalSpeed", rb.linearVelocity.y);
    }

    // --- [추가 기능] 돌격 코루틴 ---
    IEnumerator DashRoutine()
    {
        canDash = false;
        isDashing = true;

        float dashDir = sr.flipX ? -1f : 1f;
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f; 
        rb.linearVelocity = new Vector2(dashDir * dashSpeed, 0f);

        // [수정된 부분] ---------------------------------------
        // 1. 혹시라도 켜져 있을지 모르는 이전 트리거를 끕니다. (중복 방지)
        anim.ResetTrigger("DoAttack");
        
        // 2. 그 다음 진짜 공격 신호를 보냅니다.
        anim.SetTrigger("DoAttack");
        // ----------------------------------------------------

        yield return new WaitForSeconds(dashDuration);

        rb.gravityScale = originalGravity;
        rb.linearVelocity = Vector2.zero;
        isDashing = false;

        // [수정된 부분] ---------------------------------------
        // 3. 돌격이 끝났으니 공격 신호를 확실하게 꺼줍니다.
        anim.ResetTrigger("DoAttack");
        // ----------------------------------------------------

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    // --- [추가 기능] 충돌 처리 (몸통 박치기) ---
    void OnCollisionEnter2D(Collision2D collision)
    {
        // 내가 돌격 중이고(isDashing), 부딪힌 게 적(Enemy)이라면?
        if (isDashing && collision.gameObject.CompareTag("Enemy"))
        {
            Destroy(collision.gameObject); // 적 삭제
            Debug.Log("돌격으로 적 처치!");
        }
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        // 내가 돌격 중이고(isDashing), 닿은 게 적(Enemy)이라면?
        if (isDashing && other.CompareTag("Enemy"))
        {
            Destroy(other.gameObject); // 적 삭제
            Debug.Log("돌격으로 (공중) 적 처치!");
        }
    }

    public void ApplyKnockback(Vector2 knockbackForce)
    {
        StopAllCoroutines();
        // 넉백 당하면 돌격 상태도 강제 해제
        isDashing = false; 
        isKnockedBack = true;
        
        rb.gravityScale = defaultGravity;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(knockbackForce, ForceMode2D.Impulse);
        StartCoroutine(ResetKnockbackRoutine());
    }

    IEnumerator ResetKnockbackRoutine()
    {
        yield return new WaitForSeconds(0.3f);
        isKnockedBack = false;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector2 boxOrigin = (Vector2)transform.position + Vector2.up * 0.3f;
        Gizmos.DrawWireCube(boxOrigin + Vector2.down * (castDistance + 0.3f), boxSize);
    }
}