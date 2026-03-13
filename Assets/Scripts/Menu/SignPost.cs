using UnityEngine;
using System.Collections;

public class SignPost : MonoBehaviour
{
    [Header("UI 연결")]
    public GameObject interactionUI;    // "읽으시겠습니까? (B)" 텍스트
    public GameObject signDetailUI;     // 큰 나무판자 팝업
    
    [Header("아웃라인 설정")]
    public GameObject outlineObject;    // 표지판 가장자리를 담당할 자식 오브젝트

    private Animator signAnimator;
    private bool isPlayerNearby = false;
    private bool isViewingDetail = false;

    void Start()
    {
        // 모든 UI와 효과 초기화
        if (interactionUI != null) interactionUI.SetActive(false);
        if (outlineObject != null) outlineObject.SetActive(false);
        if (signDetailUI != null)
        {
            signDetailUI.SetActive(false);
            signAnimator = signDetailUI.GetComponent<Animator>();
        }
    }

    void Update()
    {
        // 1. 플레이어가 근처에 있고, 아직 큰 표지판을 보지 않는 상태일 때
        if (isPlayerNearby && !isViewingDetail)
        {
            if (Input.GetKeyDown(KeyCode.B))
            {
                OpenSign();
            }
        }
        // 2. 이미 큰 표지판을 보고 있는 상태일 때
        else if (isViewingDetail)
        {
            if (Input.GetKeyDown(KeyCode.B))
            {
                StartCoroutine(CloseSignRoutine());
            }
        }
    }

    // 표지판 크게 보기
    void OpenSign()
    {
        isViewingDetail = true;
        signDetailUI.SetActive(true);
        interactionUI.SetActive(false);
        
        if (signAnimator != null)
        {
            signAnimator.SetTrigger("Show");
        }
        
        Time.timeScale = 0f; // 게임 일시정지
    }

    // 표지판 닫기 (내려가는 애니메이션 대기)
    IEnumerator CloseSignRoutine()
    {
        isViewingDetail = false;
        
        if (signAnimator != null)
        {
            signAnimator.SetTrigger("Hide");
        }
        
        // 애니메이션이 내려가는 시간만큼 대기 (UnscaledTime 기준)
        yield return new WaitForSecondsRealtime(0.5f); 
        
        signDetailUI.SetActive(false);
        
        // 아직 플레이어가 근처에 있다면 안내 문구를 다시 띄워줌
        if (isPlayerNearby)
        {
            interactionUI.SetActive(true);
        }
        
        Time.timeScale = 1f; // 게임 다시 시작
    }

    // 플레이어가 근처에 옴
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNearby = true;
            interactionUI.SetActive(true);
            
            // 노란색 아웃라인 켜기
            if (outlineObject != null)
            {
                outlineObject.SetActive(true);
            }
        }
    }

    // 플레이어가 멀어짐
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNearby = false;
            isViewingDetail = false;
            
            // 모든 코루틴 중단 및 UI 초기화
            StopAllCoroutines();
            interactionUI.SetActive(false);
            signDetailUI.SetActive(false);
            
            // 노란색 아웃라인 끄기
            if (outlineObject != null)
            {
                outlineObject.SetActive(false);
            }
            
            Time.timeScale = 1f;
        }
    }
}