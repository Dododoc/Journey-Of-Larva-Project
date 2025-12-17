using UnityEngine;

public class FluidTrap : MonoBehaviour
{
    [Header("Settings")]
    public float duration = 5.0f;       // 장판 유지 시간
    public float slowMultiplier = 0.4f; // 이동 속도 감소 비율
    public SpriteRenderer spriteRenderer; // ★ 인스펙터 연결 또는 자동 찾기

    void Start()
    {
        if(spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        Destroy(gameObject, duration); 
    }

    void Update()
    {
        // ★ 서서히 투명해지기 (1 -> 0)
        if (spriteRenderer != null)
        {
            float alpha = Mathf.MoveTowards(spriteRenderer.color.a, 0f, Time.deltaTime / duration);
            
            Color newColor = spriteRenderer.color;
            newColor.a = alpha;
            spriteRenderer.color = newColor;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        ApplyDebuff(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        ApplyDebuff(other);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            BeetleController beetle = other.GetComponent<BeetleController>();
            if (beetle != null)
            {
                beetle.SetDebuff(false, 1.0f); 
            }
        }
    }

    void ApplyDebuff(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            BeetleController beetle = other.GetComponent<BeetleController>();
            if (beetle != null)
            {
                beetle.SetDebuff(true, slowMultiplier); 
            }
        }
    }
}