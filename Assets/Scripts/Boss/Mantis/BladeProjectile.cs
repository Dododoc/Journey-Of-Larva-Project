using UnityEngine;

public class BladeProjectile : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 10f;      
    public float lifetime = 3.0f;  
    public float damage = 15f;     

    private Vector2 moveDirection = Vector2.right; // 기본값

    // ★ [핵심] 보스가 계산해준 방향(dir)을 받아서 저장
    public void Setup(Vector2 dir)
    {
        moveDirection = dir.normalized; // 방향 벡터 저장

        // 날아가는 방향을 바라보도록 회전
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // ★ [핵심] World 좌표계 기준으로 이동 (보스가 뒤집혀도 상관없이 플레이어 쪽으로 감)
        transform.Translate(moveDirection * speed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) 
        {
            PlayerStats playerStats = other.GetComponent<PlayerStats>();
            if (playerStats != null) playerStats.TakeDamage(damage);
            Destroy(gameObject); 
        }
        else if (other.CompareTag("Enemy") || other.CompareTag("Trap")) return; 
        else if (other.CompareTag("Ground")) Destroy(gameObject);
    }
}