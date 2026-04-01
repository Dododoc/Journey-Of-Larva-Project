using UnityEngine;

public class LeafItem : MonoBehaviour
{
    [Header("상호작용 효과")]
    public GameObject outlineEffect; // 테두리 효과

    [Header("부유(둥둥) 효과")]
    public bool useFloating = true;   
    public float floatSpeed = 2.0f;    
    public float floatAmplitude = 0.05f; 

    private float startY; 
    private bool hasLanded = false; 
    private Rigidbody2D rb;
    private bool isPlayerNearby = false; 
    [Header("아이템 설정")]
    public float expAmount = 100f; // ★ 유니티 화면에서 경험치 양을 조절할 수 있게 뚫어줍니다!

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // 시작할 때 테두리는 숨겨둡니다.
        if (outlineEffect != null) outlineEffect.SetActive(false);
    }

    void Update()
    {
        // 1. 둥둥 효과 (바닥에 닿은 후)
        if (useFloating && hasLanded)
        {
            float newY = startY + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        // 2. 플레이어가 감지 범위 내에 있고 X키를 누르면 줍기!
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.X))
        {
            PlayerStats playerStats = FindFirstObjectByType<PlayerStats>();
            Collect(playerStats);
        }
    }

    // [몸통 충돌] 바닥에 닿았을 때 중력 끄기
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
            
            // ★ [수정됨] 여기서 isTrigger를 강제로 켜던 낡은 코드를 삭제했습니다!
            
            startY = transform.position.y + 0.1f; 
        }
    }

    // ==========================================
    // ★ [레이더망 감지] 감지 범위(동그라미 센서)에 들어오면 UI 켜기!
    // ==========================================
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // ★ [수정] 오직 'Player' 태그를 가진 물체만 로그를 찍고 로직을 실행합니다!
        if (collision.CompareTag("Player"))
        {
            Debug.Log("나뭇잎 레이더에 플레이어 포착!"); // 이제 땅은 로그를 찍지 않습니다.
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
            if (outlineEffect != null) outlineEffect.SetActive(false); // 테두리 OFF

            // 캐릭터 머리 위 텍스트 OFF
            PlayerStats player = collision.GetComponent<PlayerStats>();
            if (player != null) player.RemoveNearbyItem(); // ★ 변경: -1 빼!
        }
    }

    public void Collect(PlayerStats stats)
    {
        if (stats != null)
        {
            stats.GainExp(expAmount); 
            Debug.Log("나뭇잎 획득! 경험치가 올랐습니다.");
        }
        
        // ★ 파괴되기 전에 반드시 머리 위 글자를 꺼줍니다.
        PlayerStats player = FindFirstObjectByType<PlayerStats>();
        if (player != null) player.RemoveNearbyItem(); // ★ 변경: -1 빼!
        if (QuestManager.instance != null)
        {
            QuestManager.instance.CompleteQuest(1); // 1번 퀘스트 완료! (번호는 유저님 퀘스트 순서에 맞게 수정하세요)
        }

        Destroy(gameObject); 
    }
}