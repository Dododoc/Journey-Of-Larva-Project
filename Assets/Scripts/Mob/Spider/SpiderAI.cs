using UnityEngine;
using System.Collections; // ★ 이 줄이 있어야 IEnumerator 에러가 사라집니다.

public class SpiderAI : MonoBehaviour
{
    [Header("Detection & Attack")]
    public float detectionRange = 10f;
    public float fireRate = 2f;
    private float nextFireTime;

    [Header("References")]
    public GameObject toxicPrefab;
    public Transform firePoint; 
    public LineRenderer webLine;
    public float webTopY = 10f; 
    public LayerMask groundLayer;

    private Transform player;
    private SpriteRenderer sr;
    private Animator anim;
    private EnemyStats stats; //

    // 넉백 연출을 위한 변수
    private bool isKnockedBack = false;
    private Vector3 originalPos;

    void Start()
    {
        player = GameObject.FindWithTag("Player")?.transform;
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        stats = GetComponent<EnemyStats>(); //
        originalPos = transform.position;

        if (webLine != null)
        {
            webLine.positionCount = 2;
            webLine.useWorldSpace = false; //
        }
    }

    void Update()
    {
        if (isKnockedBack) return; // 넉백 중에는 공격/추적 중지

        UpdateSpiderweb();

        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);
        sr.flipX = (player.position.x > transform.position.x);

        if (distance <= detectionRange && Time.time >= nextFireTime)
        {
            Attack();
            nextFireTime = Time.time + fireRate;
        }
    }

    void UpdateSpiderweb()
    {
        if (webLine != null)
        {
            webLine.SetPosition(0, new Vector3(0, webTopY, 0));
            webLine.SetPosition(1, Vector3.zero); //
        }
    }

    // ★ 거미 전용 넉백 함수
    public void ApplyKnockback(Vector2 force)
    {
        StopCoroutine("SwingRoutine");
        StartCoroutine(SwingRoutine(force.x));
    }

    // ★ IEnumerator 에러가 났던 부분
    IEnumerator SwingRoutine(float pushForce)
    {
        isKnockedBack = true;
        float elapsed = 0f;
        float duration = 0.5f;
        float swingMagnitude = pushForce * 0.05f; // 흔들림 강도 조절

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // 사인파를 이용해 좌우로 감쇄하며 흔들리는 연출
            float xOffset = Mathf.Sin(elapsed * 20f) * swingMagnitude * (1 - (elapsed / duration));
            transform.position = originalPos + new Vector3(xOffset, 0, 0);
            yield return null;
        }

        transform.position = originalPos;
        isKnockedBack = false;
    }

    void Attack()
    {
        if(anim != null) anim.SetTrigger("Attack");
        Invoke("ShootToxic", 0.3f); 
    }

    void ShootToxic()
    {
        if (player == null) return;

        GameObject toxic = Instantiate(toxicPrefab, firePoint.position, Quaternion.identity);
        ToxicProjectile projectileScript = toxic.GetComponent<ToxicProjectile>();
        
        if (projectileScript != null)
        {
            Vector2 direction = (player.position - firePoint.position).normalized;
            projectileScript.Setup(direction, stats.attackDamage); //
        }
    }
}