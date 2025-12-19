using UnityEngine;
using System.Collections; 

public class SpiderAI : MonoBehaviour
{
    [Header("Detection & Attack")]
    public float detectionRange = 10f;
    public float fireRate = 2f;
    
    // ★ [추가] 침 투사체만의 데미지를 따로 설정 (기본값 15)
    public float projectileDamage = 15f; 

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
    private EnemyStats stats; 

    private bool isKnockedBack = false;
    private Vector3 originalPos;

    void Start()
    {
        player = GameObject.FindWithTag("Player")?.transform;
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        stats = GetComponent<EnemyStats>(); 
        originalPos = transform.position;

        if (webLine != null)
        {
            webLine.positionCount = 2;
            webLine.useWorldSpace = false; 
        }
    }

    void Update()
    {
        if (isKnockedBack) return; 

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
            webLine.SetPosition(1, Vector3.zero); 
        }
    }

    public void ApplyKnockback(Vector2 force)
    {
        StopCoroutine("SwingRoutine");
        StartCoroutine(SwingRoutine(force.x));
    }

    IEnumerator SwingRoutine(float pushForce)
    {
        isKnockedBack = true;
        float elapsed = 0f;
        float duration = 0.5f;
        float swingMagnitude = pushForce * 0.05f; 

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
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
            
            // ★ [수정] stats.attackDamage 대신, 위에서 설정한 projectileDamage 사용
            projectileScript.Setup(direction, projectileDamage); 
        }
    }
}