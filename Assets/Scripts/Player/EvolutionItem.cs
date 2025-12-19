using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

public class EvolutionItem : MonoBehaviour
{
    [Header("변신 설정")]
    public GameObject nextCharacterPrefab; 
    public GameManager.CharacterType typeToChange; 

    [Header("애니메이션 설정")]
    public string evolutionTriggerName = "ToAnt"; 
    public float animationDuration = 2.0f;

    [Header("UI 설정")]
    [Tooltip("자식으로 넣어둔 안내 텍스트 오브젝트 (예: 'R키를 누르세요')")]
    public GameObject guideTextObject; 

    // 내부 변수
    private bool canEvolve = false;
    private bool isEvolving = false; 
    private GameObject targetPlayer; 

    private SpriteRenderer itemRenderer;
    private Collider2D itemCollider;

    private void Start()
    {
        itemRenderer = GetComponent<SpriteRenderer>();
        itemCollider = GetComponent<Collider2D>();

        // 시작할 때는 안내 문구를 안 보이게 끔
        if (guideTextObject != null)
        {
            guideTextObject.SetActive(false);
        }
    }

    private void Update()
    {
        // 범위 안 + R키 + 변신 중 아님
        if (canEvolve && Input.GetKeyDown(KeyCode.R) && !isEvolving)
        {
            StartCoroutine(EvolveProcess());
        }
    }

    private IEnumerator EvolveProcess()
    {
        isEvolving = true; // 진화 시작

        // 1. 아이템 숨기기 (먹은 효과)
        if (itemRenderer != null) itemRenderer.enabled = false;
        if (itemCollider != null) itemCollider.enabled = false;

        // ★ 중요: 먹었으니까 안내 문구도 즉시 끔
        if (guideTextObject != null) guideTextObject.SetActive(false);

        Debug.Log($"진화 시작! 애니메이션: {evolutionTriggerName}");

        // 2. 애니메이션 실행
        if (targetPlayer != null)
        {
            Animator playerAnim = targetPlayer.GetComponent<Animator>();
            if (playerAnim != null)
            {
                playerAnim.SetTrigger(evolutionTriggerName);
            }
        }

        // 3. 애니메이션 대기
        yield return new WaitForSeconds(animationDuration);

        // 4. 변신 수행
        PerformTransformation();
    }

    private void PerformTransformation()
    {
        if (targetPlayer == null)
        {
            Destroy(gameObject);
            return;
        }

        if (nextCharacterPrefab != null)
        {
            Vector3 spawnPos = targetPlayer.transform.position;
            Quaternion spawnRot = targetPlayer.transform.rotation;

            Destroy(targetPlayer);

            GameObject newPlayer = Instantiate(nextCharacterPrefab, spawnPos, spawnRot);
            newPlayer.tag = "Player";

            var cineCam = FindObjectOfType<CinemachineCamera>();
            if (cineCam != null) cineCam.Follow = newPlayer.transform;
            if (GameManager.instance != null) 
            {
                // 1. 캐릭터 타입 변경
                GameManager.instance.currentCharacter = typeToChange;
                
                // 2. ★ 경험치와 레벨 강제 초기화 호출 ★
                // 이걸 해야 새로 태어난 플레이어가 0부터 시작합니다.
                GameManager.instance.ResetGlobalStats();
            }
            
            if (GameManager.instance != null) GameManager.instance.currentCharacter = typeToChange;
        }

        Debug.Log("진화 성공!");
        Destroy(gameObject); 
    }

    // --- 충돌 감지 ---

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            canEvolve = true;
            targetPlayer = collision.gameObject;

            // ★ 범위에 들어오면 텍스트 켜기
            if (guideTextObject != null) guideTextObject.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // 진화 중이면 무시 (플레이어 정보 유지)
        if (isEvolving) return;

        if (collision.CompareTag("Player"))
        {
            canEvolve = false;
            targetPlayer = null;

            // ★ 범위에서 나가면 텍스트 끄기
            if (guideTextObject != null) guideTextObject.SetActive(false);
        }
    }
}