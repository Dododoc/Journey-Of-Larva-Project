using UnityEngine;

public class BouncyMushroom : MonoBehaviour
{
    [Header("설정")]
    public float bounceForce = 20f; // 튕겨 올라가는 힘 (15~25 추천)

    [Header("효과음 (선택사항)")]
    public AudioClip bounceSound;
    private AudioSource audioSource;

    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>(); // 애니메이션이 있다면 사용
        
        if (bounceSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 1. 플레이어와 부딪혔는지 확인
        if (collision.gameObject.CompareTag("Player"))
        {
            // 2. 플레이어가 버섯 '위'에서 떨어지면서 닿았는지 확인
            // (플레이어 발바닥이 버섯 중심보다 위에 있고 + 떨어지는 속도일 때)
            Rigidbody2D rb = collision.gameObject.GetComponent<Rigidbody2D>();
            
            if (rb != null && collision.transform.position.y > transform.position.y)
            {
                Bounce(rb);
            }
        }
    }

    void Bounce(Rigidbody2D rb)
    {
        // ★ 핵심: 기존 낙하 속도를 0으로 초기화 (이래야 언제 밟아도 똑같은 높이로 튐)
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

        // 위쪽으로 힘을 팍! 줌 (Impulse 모드)
        rb.AddForce(Vector2.up * bounceForce, ForceMode2D.Impulse);

        // 효과음 재생
        if (audioSource != null && bounceSound != null)
            audioSource.PlayOneShot(bounceSound);

        // 애니메이션 (꿀렁거리는 모션이 있다면 트리거 실행)
        if (anim != null)
            anim.SetTrigger("DoBounce");
            
        Debug.Log("Boing! 버섯 점프!");
    }
}