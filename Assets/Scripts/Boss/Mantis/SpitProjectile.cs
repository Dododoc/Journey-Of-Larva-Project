using UnityEngine;

public class SpitProjectile : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 10f; // 날아가는 속도
    public GameObject trapPrefab; // 바닥에 깔릴 독 장판 프리팹

    private Vector3 targetPos;

    public void Setup(Vector3 target, GameObject trap)
    {
        targetPos = target;
        trapPrefab = trap;
        
        // 타겟 방향으로 회전
        Vector3 dir = (target - transform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void Update()
    {
        // 타겟 위치를 향해 이동
        transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

        // 도착했으면 장판 깔고 삭제
        if (Vector3.Distance(transform.position, targetPos) < 0.1f)
        {
            SpawnTrap();
        }
    }

    void SpawnTrap()
    {
        if (trapPrefab != null)
        {
            Instantiate(trapPrefab, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 바닥에 닿아도 장판 깔고 삭제
        if (other.CompareTag("Ground"))
        {
            SpawnTrap();
        }
    }
}