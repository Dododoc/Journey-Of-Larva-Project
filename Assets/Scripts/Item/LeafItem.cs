using UnityEngine;

public class LeafItem : MonoBehaviour
{
    [Header("Item Stats")]
    public float expAmount = 30f;
    
    [Header("Outline Settings")]
    public Color highlightColor = Color.yellow;
    public float maxOutlineWidth = 2f; // 테두리 두께
    public float detectionRange = 2.5f; // 수집 인식 범위

    private Material leafMaterial;
    private Transform playerTransform;

    void Start()
{
    // 기존의 플레이어 찾기 및 물리 효과 로직만 남김
    GameObject player = GameObject.FindWithTag("Player");
    if (player != null) playerTransform = player.transform;

    Rigidbody2D rb = GetComponent<Rigidbody2D>();
    if (rb != null)
    {
        float randomX = Random.Range(-3f, 3f);
        float randomY = Random.Range(5f, 8f);
        rb.AddForce(new Vector2(randomX, randomY), ForceMode2D.Impulse);
    }
}
    void Awake()
    {
        // Awake에서 미리 머티리얼을 세팅하여 첫 프레임부터 적용되게 합니다.
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            // 인스턴스 머티리얼 생성 및 초기화
            leafMaterial = sr.material;
            leafMaterial.SetFloat("_OutlineWidth", 0f); 
            leafMaterial.SetColor("_OutlineColor", highlightColor);
        }
    }
    void Update()
    {
        if (playerTransform == null || leafMaterial == null) return;

        // 플레이어와의 거리 체크
        float distance = Vector2.Distance(transform.position, playerTransform.position);

        if (distance <= detectionRange)
        {
            // 범위 안이면 쉐이더의 외곽선 두께 증가
            leafMaterial.SetFloat("_OutlineWidth", maxOutlineWidth);
        }
        else
        {
            // 범위 밖이면 외곽선 제거
            leafMaterial.SetFloat("_OutlineWidth", 0);
        }
    }

    public void Collect(PlayerStats stats)
    {
        if (stats != null)
        {
            stats.GainExp(expAmount); // 경험치 획득
            Destroy(gameObject);
        }
    }
}