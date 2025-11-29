using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("체력 설정")]
    public float maxHealth = 4f;
    public float currentHealth;
    
    [Header("UI 연결")]
    public Image frontHealthBar; // 빨간색 (즉시 감소)
    public Image backHealthBar;  // 노란색 (천천히 감소)
    public float chipSpeed = 2f; // 잔상이 줄어드는 속도

    private Vector3 startPos;
    private Rigidbody2D rb;
    private float lerpTimer; // 부드러운 움직임을 위한 타이머

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
        startPos = transform.position;
        
        // 시작할 때 UI 초기화
        UpdateHealthUI(); 
    }

    void Update()
    {
        // 1. 체력 UI 업데이트 (매 프레임 부드럽게)
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHealthUI();
    }

    // 가시 밟았을 때
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Trap"))
        {
            TakeDamage(1f);
        }
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        lerpTimer = 0f; // 데미지 입으면 타이머 리셋 (잔상 효과 시작)

        if (currentHealth <= 0)
        {
            Respawn();
        }
    }

    void Respawn()
    {
        transform.position = startPos;
        rb.linearVelocity = Vector2.zero;
        currentHealth = maxHealth;
        
        // 리스폰 할 때는 잔상 없이 즉시 꽉 채우기
        frontHealthBar.fillAmount = 1;
        backHealthBar.fillAmount = 1;
    }

    void UpdateHealthUI()
    {
        float fillF = frontHealthBar.fillAmount;
        float fillB = backHealthBar.fillAmount;
        float hFraction = currentHealth / maxHealth; // 목표 %

        // 1. 빨간색 바는 즉시 줄어듦 (타격감)
        if (fillB > hFraction) 
        {
            frontHealthBar.fillAmount = hFraction; // 빨간색은 바로 깎임
            backHealthBar.fillAmount = Mathf.Lerp(fillB, hFraction, Time.deltaTime * chipSpeed); // 노란색은 천천히
        }
        
        // (만약 힐을 해서 체력이 늘어날 때도 대비한 코드)
        if (fillF < hFraction)
        {
            backHealthBar.fillAmount = hFraction;
            frontHealthBar.fillAmount = Mathf.Lerp(fillF, hFraction, Time.deltaTime * chipSpeed);
        }
    }
}