using UnityEngine;

public class LeafItem : MonoBehaviour
{
    [Header("상호작용 효과")]
    public GameObject outlineEffect; 

    [Header("부유(둥둥) 효과")]
    public bool useFloating = true;   
    public float floatSpeed = 2.0f;    
    public float floatAmplitude = 0.05f; 

    private float startY; 
    private bool hasLanded = false; 
    private Rigidbody2D rb;
    private bool isPlayerNearby = false; 
    [Header("아이템 설정")]
    public float expAmount = 100f; 

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (outlineEffect != null) outlineEffect.SetActive(false);
    }

    void Update()
    {
        if (useFloating && hasLanded)
        {
            float newY = startY + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        // ★ [수정됨] 상호작용 키를 F로 변경했습니다.
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.F))
        {
            PlayerStats playerStats = FindFirstObjectByType<PlayerStats>();
            Collect(playerStats);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!hasLanded && collision.gameObject.CompareTag("Ground"))
        {
            hasLanded = true; 
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.linearVelocity = Vector2.zero;
            }
            startY = transform.position.y + 0.1f; 
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNearby = true;
            if (outlineEffect != null) outlineEffect.SetActive(true);

            PlayerStats player = collision.GetComponent<PlayerStats>();
            // ★ 상호작용 키(F)를 누르라는 팝업을 띄웁니다.
            if (player != null) player.AddNearbyItem();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNearby = false;
            if (outlineEffect != null) outlineEffect.SetActive(false);

            PlayerStats player = collision.GetComponent<PlayerStats>();
            if (player != null) player.RemoveNearbyItem(); 
        }
    }

    public void Collect(PlayerStats stats)
    {
        if (stats != null)
        {
            stats.GainExp(expAmount); 
        }
        
        PlayerStats player = FindFirstObjectByType<PlayerStats>();
        if (player != null) player.RemoveNearbyItem(); 
        
        if (QuestManager.instance != null)
        {
            QuestManager.instance.CompleteQuest(1); 
        }

        Destroy(gameObject); 
    }
}