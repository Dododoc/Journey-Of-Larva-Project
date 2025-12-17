using UnityEngine;

public class SandProjectile : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        // 땅(Ground)이나 플레이어(Player)에 닿으면 사라짐
        if (other.CompareTag("Ground") || other.CompareTag("Player"))
        {
            // 나중에 여기에 '퍼석'하는 이펙트 추가 가능
            Destroy(gameObject); 
        }
    }

    // 화면 밖으로 나가면 삭제 (메모리 관리)
    void OnBecameInvisible() 
    {
        Destroy(gameObject);
    }
}