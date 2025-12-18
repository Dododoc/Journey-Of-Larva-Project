using UnityEngine;
using System.Collections;

public class SpecialTree : MonoBehaviour
{
    [Header("Sprites & Animation")]
    public Animator anim;
    public SpriteRenderer sr;

    [Header("Drop Settings")]
    public GameObject greenLeafPrefab;
    public GameObject goldLeafPrefab;
    
    // ★ [수정] 랜덤 드롭 범위를 위한 설정
    public Vector2 dropAreaCenter; // 나무 중심으로부터의 상대적 중심점
    public Vector2 dropAreaSize;   // 드롭 구역의 가로, 세로 크기
    
    [Range(0, 100)] public float goldChance = 30f;

    [Header("Tree Stats")]
    public int maxHits = 4;
    private int currentHits = 0;
    private bool isCollapsed = false;
    void Awake()
    {
        // ★ 추가: 인스펙터에서 깜빡하고 연결 안 했을 때를 대비해 자동으로 컴포넌트를 가져옵니다.
        if (anim == null)
        {
            anim = GetComponent<Animator>();
        }

        // SpriteRenderer도 마찬가지로 자동 할당하면 안전합니다.
        if (sr == null)
        {
            sr = GetComponent<SpriteRenderer>();
        }
    }

        // SpecialTree.cs의 OnCollisionEnter2D 수정
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isCollapsed) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            Larva_PlayerController player = collision.gameObject.GetComponent<Larva_PlayerController>();
            
            // ★ 핵심: 플레이어의 IsDashing 프로퍼티가 true일 때만 타격으로 인정
            if (player != null && player.IsDashing)
            {
                TakeHit();
            }
        }
    }

    void TakeHit()
    {
        currentHits++;
        
        if (currentHits < maxHits)
        {
            anim.SetTrigger("DoHit");
            DropLeaf();
        }
        else
        {
            StartCoroutine(CollapseRoutine());
        }
    }

    void DropLeaf()
    {
        GameObject prefab = (Random.value * 100f <= goldChance) ? goldLeafPrefab : greenLeafPrefab;
        
        if (prefab != null)
        {
            // ★ [핵심] 랜덤 위치 계산
            Vector3 randomPos = new Vector3(
                transform.position.x + dropAreaCenter.x + Random.Range(-dropAreaSize.x / 2, dropAreaSize.x / 2),
                transform.position.y + dropAreaCenter.y + Random.Range(-dropAreaSize.y / 2, dropAreaSize.y / 2),
                0
            );

            Instantiate(prefab, randomPos, Quaternion.identity);
        }
    }

    IEnumerator CollapseRoutine()
{
    isCollapsed = true;

    // 1. 플레이어 위치 확인 (반대 방향 계산)
    // Larva_PlayerController가 이미 충돌 시점에 확인되었다고 가정합니다.
    GameObject player = GameObject.FindWithTag("Player"); 
    if (player != null)
    {
        float playerX = player.transform.position.x;
        float treeX = transform.position.x;

        // 플레이어가 나무보다 오른쪽에 있으면
        if (playerX > treeX)
        {
            // 나무를 좌우 반전시켜 왼쪽으로 쓰러지게 함
            sr.flipX = true;
        }
        else
        {
            // 플레이어가 왼쪽에 있으면 기본 방향(오른쪽)으로 쓰러짐
            sr.flipX = false;
        }
    }

    // 2. 애니메이션 실행
    anim.SetTrigger("DoFall"); 
    
    // 쓰러질 때 보너스 나뭇잎 드롭
    for(int i = 0; i < 3; i++) DropLeaf();

    yield return new WaitForSeconds(2.0f); 

    // 3. 페이드 아웃
    float fadeTime = 2.0f;
    float timer = 0f;
    while (timer < fadeTime)
    {
        timer += Time.deltaTime;
        float alpha = Mathf.Lerp(1f, 0f, timer / fadeTime);
        if (sr != null) sr.color = new Color(1, 1, 1, alpha);
        yield return null;
    }

    Destroy(gameObject);
}

    // ★ [추가] 에디터에서 드롭 범위를 시각적으로 표시 (초록색 사각형)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 center = new Vector3(transform.position.x + dropAreaCenter.x, transform.position.y + dropAreaCenter.y, 0);
        Gizmos.DrawWireCube(center, new Vector3(dropAreaSize.x, dropAreaSize.y, 0.1f));
    }
}