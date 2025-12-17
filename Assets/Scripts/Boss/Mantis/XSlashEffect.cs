using UnityEngine;
using System.Collections;
public class XSlashEffect : MonoBehaviour
{
    [Header("Visual Settings")]
    public float duration = 3.0f; // 효과 유지 시간
    
    // public으로 열어두되, 연결 안 해도 알아서 찾게 만듭니다.
    public SpriteRenderer spriteRenderer; 

    [Header("Combat Settings")]
    public float damage = 15f; 

    private float currentTime = 0f;
    private float startAlpha = 0.7f;

    void Start()
    {
        // 만약 인스펙터에서 연결을 안 했다면, 내 오브젝트에서 컴포넌트를 가져와라!
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // 그래도 없다면(스프라이트가 없는 오브젝트라면) 에러 방지
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = startAlpha; 
            spriteRenderer.color = c;
        }
    }

    private void Update()
    {
        // 스프라이트 렌더러가 없으면 아무것도 안 함
        if (spriteRenderer == null) return;

        // 1. 시간 누적
        currentTime += Time.deltaTime;

        // 2. 진행률 계산 (0 ~ 1)
        // 시간이 지날수록 0.7에서 0으로 변함
        float newAlpha = Mathf.Lerp(startAlpha, 0f, currentTime / duration);

        Color newColor = spriteRenderer.color;
        newColor.a = newAlpha;
        spriteRenderer.color = newColor;

        // 3. 시간이 다 되면 삭제
        if (currentTime >= duration)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var playerStats = other.GetComponent<PlayerStats>();
            
            // 플레이어를 찾았으면 바로 때리지 말고, 예약(StartCoroutine)을 겁니다.
            if (playerStats != null)
            {
                // "1초(1.0f) 뒤에 playerStats한테 데미지를 줘라"
                StartCoroutine(ApplyDamageAfterDelay(playerStats, 1.0f));
            }
        }
    }

    // 1초 딜레이를 처리하는 '코루틴' 함수 추가
    IEnumerator ApplyDamageAfterDelay(PlayerStats target, float delay)
    {
        // 1. 여기서 설정한 시간(1초)만큼 대기합니다.
        yield return new WaitForSeconds(delay);

        // 2. 1초가 지난 뒤, 맞아야 할 대상(Player)이 아직 존재하는지 확인하고 때립니다.
        if (target != null)
        {
            target.TakeDamage(damage);
            // Debug.Log("1초 뒤 푹찍!"); // 테스트용 로그
        }
    }
}