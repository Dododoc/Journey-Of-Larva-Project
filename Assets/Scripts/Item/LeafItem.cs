using UnityEngine;

public class LeafItem : MonoBehaviour
{
    public float expAmount = 30f;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // ★ Dynamic 모드인지 확인
        rb.bodyType = RigidbodyType2D.Dynamic;
        
        // 포물선을 그리며 튀어나가도록 힘을 줍니다.
        float randomX = Random.Range(-3f, 3f); // 좌우 랜덤
        float randomY = Random.Range(5f, 8f); // 위로 튀어 오름
        rb.AddForce(new Vector2(randomX, randomY), ForceMode2D.Impulse);
        
        // 회전 효과 추가 (팔랑거리는 느낌)
        rb.AddTorque(Random.Range(-10f, 10f), ForceMode2D.Impulse);
    }

    public void Collect(PlayerStats stats)
    {
        if (stats != null)
        {
            stats.GainExp(expAmount);
            Destroy(gameObject);
        }
    }
}