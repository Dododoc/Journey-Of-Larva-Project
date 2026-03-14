using UnityEngine;

public class ToxicGround : MonoBehaviour
{
    [Header("Toxic Settings")]
    public float duration = 4f;        // 장판 지속 시간
    public float damageInterval = 0.5f;// 데미지를 입히는 주기 (0.5초마다 독 갱신)
    
    // ★ [수정] 장판의 총 독 데미지와 지속 시간
    public float poisonTotalDamage = 15f;    
    public float poisonDuration = 3.0f;

    private float timer;

    void Start()
    {
        Destroy(gameObject, duration);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            timer += Time.deltaTime;
            // 0.5초(damageInterval)마다 독을 새롭게 갱신
            if (timer >= damageInterval)
            {
                PlayerStats playerStats = other.GetComponent<PlayerStats>();
                if (playerStats != null)
                {
                    // ★ [수정] 밟고 있는 동안 계속해서 독(ApplyPoison) 상태를 부여
                    playerStats.ApplyPoison(poisonTotalDamage, poisonDuration);
                }
                timer = 0f;
            }
        }
    }
}