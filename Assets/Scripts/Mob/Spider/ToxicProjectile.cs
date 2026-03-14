using UnityEngine;

public class ToxicProjectile : MonoBehaviour
{
    public float speed = 12f;
    public GameObject groundEffectPrefab;
    
    // ★ [추가] 독 지속 시간 (인스펙터에서 조절 가능)
    public float poisonDuration = 3.0f; 
    
    private Vector2 moveDirection;
    private float damage;

    public void Setup(Vector2 dir, float dmg)
    {
        moveDirection = dir;
        damage = dmg;

        // 투사체 회전 (왼쪽 스프라이트 기준)
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle + 180f);
    }

    void Update()
    {
        // 정해진 방향으로 일직선 이동
        transform.Translate(Vector3.left * speed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 1. 지면(Ground)에 닿았을 때 장판 생성
        if (other.CompareTag("Ground"))
        {
            Explode(transform.position);
        }
        // 2. 플레이어에게 직격했을 때
        else if (other.CompareTag("Player"))
        {
            PlayerStats pStats = other.GetComponent<PlayerStats>();
            if (pStats != null) 
            {
                // ★ [수정] TakeDamage 대신 ApplyPoison을 호출하여 도트 딜 부여!
                pStats.ApplyPoison(damage, poisonDuration);
            }
            
            Destroy(gameObject); 
        }
    }

    void Explode(Vector3 spawnPos)
    {
        if (groundEffectPrefab != null)
        {
            Instantiate(groundEffectPrefab, spawnPos, Quaternion.identity); 
        }
        Destroy(gameObject);
    }
}