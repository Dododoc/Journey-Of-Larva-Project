using UnityEngine;

public class DamageDealer : MonoBehaviour
{
    public float damage = 10f;
    
    // ★ [이 부분이 빠져있어서 에러가 난 것입니다]
    public GameObject hitEffectPrefab; 

    private bool hasHit = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHit) return;

        // 플레이어와 충돌
        if (collision.CompareTag("Player"))
        {
            PlayerStats player = collision.GetComponent<PlayerStats>();
            if (player != null)
            {
                player.TakeDamage(damage);
                hasHit = true;
                SpawnEffectAndDestroy(); // 이펙트 생성 및 삭제
            }
        }
        // 땅(Ground)과 충돌 시 (모래돌의 경우)
        else if (collision.CompareTag("Ground"))
        {
            hasHit = true;
            SpawnEffectAndDestroy(); // 이펙트 생성 및 삭제
        }
    }

    void SpawnEffectAndDestroy()
    {
        // 1. 이펙트가 있으면 생성
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }

        // 2. 오브젝트 삭제
        // Rigidbody가 있는 투사체(돌멩이)만 삭제합니다. 
        // 가시(Spike)는 보통 Rigidbody가 없고 Boss가 시간 지나면 삭제하므로 여기선 건드리지 않습니다.
        if (GetComponent<Rigidbody2D>() != null) 
        {
            Destroy(gameObject); 
        }
    }
}