using UnityEngine;

public class ToxicProjectile : MonoBehaviour
{
    public float speed = 12f;
    public GameObject groundEffectPrefab;
    
    // ★ 독 지속 시간 (인스펙터에서 조절 가능)
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
                // ★ [핵심 수정] 1. 일단 뼈아픈 직격 데미지를 한 방 먹입니다!
                pStats.TakeDamage(damage);
                
                // ★ [핵심 수정] 2. 그리고 독 상태이상을 추가로 부여합니다. 
                // (독 딜은 직격 데미지의 절반인 0.5f를 곱해서 넣었습니다. 원하시면 숫자를 조절하세요!)
                pStats.ApplyPoison(damage * 0.5f, poisonDuration);
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