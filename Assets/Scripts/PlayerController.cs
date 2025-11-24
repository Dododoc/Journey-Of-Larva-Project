using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 3f;
    
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        // 1. 입력 받기 (-1, 0, 1)
        float moveInput = Input.GetAxisRaw("Horizontal");

        // 2. 물리 이동
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        // 3. 방향 뒤집기 (좌우 반전)
        if (moveInput > 0) sr.flipX = false;
        else if (moveInput < 0) sr.flipX = true;

        // 4. 애니메이션 (Speed 파라미터 사용!)
        // Mathf.Abs()는 절댓값을 만드는 함수입니다. (-1을 1로 만들어줌)
        // 즉, 왼쪽(-1)으로 가든 오른쪽(1)으로 가든 속도는 '1'이 되어 애니메이션이 재생됩니다.
        anim.SetFloat("Speed", Mathf.Abs(moveInput));
    }
}