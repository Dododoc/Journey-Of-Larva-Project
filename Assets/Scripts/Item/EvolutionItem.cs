using UnityEngine;

public class EvolutionItem : MonoBehaviour
{
    public enum ItemType { RedCloak, GoldenCasque }
    
    [Header("아이템 설정")]
    public ItemType myItemType;
    public GameObject outlineEffect; 

    [Header("물리 & 부유 효과")]
    public bool useFloating = true;   
    public float floatSpeed = 3.0f;    
    public float floatAmplitude = 0.1f; 

    private bool isPlayerNearby = false;
    private float startY; 
    private bool hasLanded = false; // ★ 땅에 닿았는지 확인하는 스위치
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (outlineEffect != null) outlineEffect.SetActive(false);
    }

    void Update()
    {
        // 1. 땅에 닿았을 때만(hasLanded) 둥둥 떠다닙니다.
        if (useFloating && hasLanded)
        {
            float newY = startY + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        // 2. 줍기 (X키)
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.X))
        {
            PickUpItem();
        }
    }

    // ==========================================
    // ★ 바닥에 닿는 순간 실행 (나뭇잎과 동일 로직)
    // ==========================================
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground") && !hasLanded)
        {
            hasLanded = true;
            
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic; // 중력 끄기
                rb.linearVelocity = Vector2.zero;
            }

            // 바닥에 닿은 위치를 시작점으로 기억
            startY = transform.position.y + 0.2f; 
        }
    }

    void PickUpItem()
    {
        // ★ [추가] 진화 창이 열릴 때 테두리를 즉시 숨깁니다!
        if (outlineEffect != null) outlineEffect.SetActive(false);

        // UIManager에게 팝업을 띄우라고 명령
        if (UIManager.instance != null) 
        {
            UIManager.instance.ShowEvolutionChoice((int)myItemType, gameObject);
        }
    }
    // ==========================================
    // ★ 플레이어 인식 (레이더망 전용)
    // ==========================================
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Debug.Log($"{gameObject.name} 레이더에 플레이어 포착!");
            isPlayerNearby = true;
            if (outlineEffect != null) outlineEffect.SetActive(true);

            PlayerStats player = collision.GetComponent<PlayerStats>();
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
    // ==========================================
    // ★ [새로 추가] 거절 버튼을 눌렀을 때 테두리를 다시 켜주는 함수
    // ==========================================
    public void SetOutline(bool isVisible)
    {
        // 플레이어가 근처에 있을 때(isPlayerNearby)만 켜지도록 안전하게 체크합니다.
        if (outlineEffect != null && isPlayerNearby) 
        {
            outlineEffect.SetActive(isVisible);
        }
    }
}