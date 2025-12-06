using UnityEngine;

public class RockController : MonoBehaviour
{
    private Rigidbody2D rb;
    private bool hasLanded = false; // 이미 땅에 닿았는지 확인

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // 무언가와 부딪혔을 때 실행
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 1. 이미 착륙했으면 무시
        if (hasLanded) return;

        // 2. 부딪힌 게 '땅(Ground)'이라면 멈춰라!
        // (Ground 레이어나 태그가 설정되어 있어야 합니다)
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground")) 
        {
            StopRock();
        }
    }

    void StopRock()
    {
        hasLanded = true;

        // ★ 핵심: 돌을 'Static(고정된 물체)'으로 바꿔서 절대 안 움직이게 만듦
        rb.bodyType = RigidbodyType2D.Static; 
        
        // (혹시 모르니 속도도 0으로 강제 초기화)
        rb.linearVelocity = Vector2.zero;
        
        Debug.Log("돌이 땅에 박혔습니다!");
    }
}