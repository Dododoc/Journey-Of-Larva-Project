using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 3f; // 이동 속도 (유니티에서 조절 가능)
    
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim;

    void Start()
    {
        // 내 몸에 붙어있는 컴포넌트들을 가져옵니다.
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        // 1. 키보드 입력 받기 (왼쪽: -1, 오른쪽: 1, 안누름: 0)
        float moveInput = Input.GetAxisRaw("Horizontal");

        // 2. 이동 실행 (Rigidbody의 속도 조절)
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        // 3. 방향 뒤집기 (왼쪽 갈 때 이미지 반전)
        if (moveInput > 0) // 오른쪽
        {
            sr.flipX = false; // 원래대로 (오른쪽 보기)
        }
        else if (moveInput < 0) // 왼쪽
        {
            sr.flipX = true;  // 뒤집기 (왼쪽 보기)
        }

        // 4. 애니메이션 전환 (걷기 <-> 대기)
        // 움직임이 0이 아니면(움직이면) IsWalk를 true로, 아니면 false로
        if (moveInput != 0)
        {
            anim.SetBool("IsWalk", true);
        }
        else
        {
            anim.SetBool("IsWalk", false);
        }
    }
}