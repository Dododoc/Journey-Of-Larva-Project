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
    private bool hasLanded = false; 
    private Rigidbody2D rb;

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
            PickUpItem();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground") && !hasLanded)
        {
            hasLanded = true;
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic; 
                rb.linearVelocity = Vector2.zero;
            }
            startY = transform.position.y + 0.2f; 
        }
    }

    void PickUpItem()
    {
        if (outlineEffect != null) outlineEffect.SetActive(false);

        if (UIManager.instance != null) 
        {
            UIManager.instance.ShowEvolutionChoice((int)myItemType, gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNearby = true;
            if (outlineEffect != null) outlineEffect.SetActive(true);

            PlayerStats player = collision.GetComponent<PlayerStats>();
            // ★ 상호작용 키(F) 팝업 띄우기
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

    public void SetOutline(bool isVisible)
    {
        if (outlineEffect != null && isPlayerNearby) 
        {
            outlineEffect.SetActive(isVisible);
        }
    }
}