using UnityEngine;

public class ToxicProjectile : MonoBehaviour
{
    public float speed = 12f;
    public GameObject groundEffectPrefab;
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

    // ★ 핵심: 무언가에 부딪혔을 때 실행
    void OnTriggerEnter2D(Collider2D other)
    {
        // 1. 지면(Ground)에 닿았을 때 장판 생성
        if (other.CompareTag("Ground"))
        {
            Explode(transform.position);
        }
        // 2. 지면에 닿기 전 플레이어에게 직격했을 때 (선택 사항)
        else if (other.CompareTag("Player"))
        {
            PlayerStats pStats = other.GetComponent<PlayerStats>();
            if (pStats != null) pStats.TakeDamage(damage);
            
            // 직격 시에도 바닥에 장판을 깔고 싶다면 지면을 찾는 로직 추가 가능
            Destroy(gameObject); 
        }
    }

    void Explode(Vector3 spawnPos)
    {
        if (groundEffectPrefab != null)
        {
            Instantiate(groundEffectPrefab, spawnPos, Quaternion.identity); //
        }
        Destroy(gameObject);
    }
}