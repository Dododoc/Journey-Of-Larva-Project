using UnityEngine;

public class EvolutionItem : MonoBehaviour
{
    public enum ItemType { RedCloak, GoldenCasque }
    
    [Header("아이템 설정")]
    public ItemType myItemType;
    
    [Header("상호작용 효과")]
    public GameObject interactUI;    
    public GameObject outlineEffect; 

    // ★ [추가됨] 부유(둥둥) 효과 설정
    [Header("부유(둥둥) 효과")]
    public bool useFloating = true;   // 부유 효과 사용 여부
    public float floatSpeed = 3.0f;    // 움직이는 속도 (높을수록 빠름)
    public float floatAmplitude = 0.1f; // 움직이는 범위 (높을수록 크게 움직임)

    private bool isPlayerNearby = false;
    private float startY; // 아이템의 원래 Y축 높이

    void Start()
    {
        if (interactUI != null) interactUI.SetActive(false);
        if (outlineEffect != null) outlineEffect.SetActive(false);

        // ★ [추가됨] 시작할 때의 Y축 위치를 기억해둡니다.
        startY = transform.position.y;
    }

    void Update()
    {
        // 1. 다가와서 F키를 눌렀을 때 상호작용 (기존 유지)
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.F))
        {
            PickUpItem();
        }

        // ==========================================
        // ★ [추가됨] 2. 부유(둥둥) 효과 처리
        // ==========================================
        if (useFloating)
        {
            // Time.time(흐른 시간)과 Sin 파동을 이용해 부드러운 오르내림을 만듭니다.
            float newY = startY + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            
            // 변경된 Y값만 적용하여 아이템 위치를 업데이트합니다.
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }

    // --- (이하 PickUpItem, OnTrigger Enter/Exit 함수는 기존과 동일) ---
    void PickUpItem()
    {
        if (UIManager.instance != null) UIManager.instance.ShowEvolutionChoice((int)myItemType);
        gameObject.SetActive(false); 
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNearby = true;
            if (interactUI != null) interactUI.SetActive(true);
            if (outlineEffect != null) outlineEffect.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNearby = false;
            if (interactUI != null) interactUI.SetActive(false);
            if (outlineEffect != null) outlineEffect.SetActive(false);
        }
    }
}