using UnityEngine;

public class ToxicGround : MonoBehaviour
{
    [Header("Toxic Settings")]
    public float duration = 4f;        // 장판 지속 시간
    public float damageInterval = 0.5f;// 데미지 입는 간격 (0.5초마다)
    
    // ★ [수정] public으로 변경하여 인스펙터에서 조절 가능하게 함
    public float poisonDamage = 5f;    

    private float timer;

    void Start()
    {
        // 기존에 damage = 5f; 라고 적혀있던 하드코딩 제거함
        Destroy(gameObject, duration);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            timer += Time.deltaTime;
            if (timer >= damageInterval)
            {
                PlayerStats playerStats = other.GetComponent<PlayerStats>();
                if (playerStats != null)
                {
                    // ★ 설정한 poisonDamage 변수 사용
                    playerStats.TakeDamage(poisonDamage);
                }
                timer = 0f;
            }
        }
    }
}