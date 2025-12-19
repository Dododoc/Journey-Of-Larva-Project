using UnityEngine;

public class ToxicGround : MonoBehaviour
{
    public float duration = 4f;
    public float damageInterval = 0.5f;
    private float damage;
    private float timer;

    void Start()
    {
        // EnemyStats로부터 데미지 수치를 가져오거나 설정 가능
        damage = 5f; // 지속 딜은 직격보다 낮게 설정
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
                    playerStats.TakeDamage(damage);
                }
                timer = 0f;
            }
        }
    }
}