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
    
    // 랜덤 드롭 범위를 위한 설정
    public Vector2 dropAreaCenter; // 나무 중심으로부터의 상대적 중심점
    public Vector2 dropAreaSize;   // 드롭 구역의 가로, 세로 크기
    
    [Range(0, 100)] public float goldChance = 30f; // 골드 나뭇잎 확률 30%

    [Header("Tree Stats")]
    public int maxHits = 4; // 4번 맞으면 쓰러짐
    public float hitCooldown = 0.5f; // 타격 간격 제한 (연타 방지)
    
    private int currentHits = 0;
    private bool isCollapsed = false;
    private float lastHitTime; // 마지막으로 맞은 시간 기록

    void Awake()
{
    // 1. 애니메이터 할당
    if (anim == null) anim = GetComponent<Animator>();

    // 2. SpriteRenderer를 먼저 할당 (순서 중요!)
    if (sr == null) sr = GetComponent<SpriteRenderer>();

    // 3. sr이 정상적으로 할당되었는지 확인 후 머티리얼 인스턴스화 진행
    if (sr != null) 
    {
        sr.material = new Material(sr.material); 
    }
    else 
    {
        Debug.LogError("SpecialTree: SpriteRenderer를 찾을 수 없습니다!");
    }
}

    // 처음 부딪혔을 때
    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleHitCheck(collision);
    }

    // 이미 붙어 있는 상태에서 대시를 눌렀을 때를 위해 추가
    void OnCollisionStay2D(Collision2D collision)
    {
        HandleHitCheck(collision);
    }

    // 타격 가능 여부를 확인하는 공통 로직
    private void HandleHitCheck(Collision2D collision)
    {
        if (isCollapsed) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            Larva_PlayerController player = collision.gameObject.GetComponent<Larva_PlayerController>();
            
            // 플레이어가 대시 중(IsDashing)이고, 쿨다운 시간이 지났는지 확인
            if (player != null && player.IsDashing)
            {
                if (Time.time - lastHitTime >= hitCooldown)
                {
                    lastHitTime = Time.time;
                    TakeHit();
                }
            }
        }
    }

    void TakeHit()
    {
        currentHits++;
        
        if (currentHits < maxHits)
        {
            // 흔들리는 애니메이션 재생 및 나뭇잎 드롭
            if (anim != null) anim.SetTrigger("DoHit");
            DropLeaf();
        }
        else
        {
            // 4번째 타격 시 쓰러짐 코루틴 시작
            StartCoroutine(CollapseRoutine());
        }
    }

    void DropLeaf()
    {
        // 확률에 따라 그린/골드 나뭇잎 결정
        GameObject prefab = (Random.value * 100f <= goldChance) ? goldLeafPrefab : greenLeafPrefab;
        
        if (prefab != null)
        {
            // 설정된 dropArea 범위 내 랜덤 위치 계산
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

        // 플레이어 위치 확인하여 반대 방향으로 쓰러지게 반전
        GameObject player = GameObject.FindWithTag("Player"); 
        if (player != null)
        {
            float playerX = player.transform.position.x;
            float treeX = transform.position.x;

            // 플레이어가 나무 오른쪽에 있으면 왼쪽으로 쓰러짐 (flipX)
            sr.flipX = (playerX > treeX);
        }

        // 쓰러지는 애니메이션 실행 및 보너스 드롭
        if (anim != null) anim.SetTrigger("DoFall"); 
        for(int i = 0; i < 3; i++) DropLeaf();

        // 애니메이션 재생 시간 대기 (약 2초)
        yield return new WaitForSeconds(2.0f); 

        // 페이드 아웃으로 사라짐
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

    // 에디터 씬 뷰에서 드롭 범위를 시각적으로 표시
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 center = new Vector3(transform.position.x + dropAreaCenter.x, transform.position.y + dropAreaCenter.y, 0);
        Gizmos.DrawWireCube(center, new Vector3(dropAreaSize.x, dropAreaSize.y, 0.1f));
    }
}