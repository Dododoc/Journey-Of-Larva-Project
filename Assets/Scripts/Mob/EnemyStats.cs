using UnityEngine;
using UnityEngine.UI;

public class EnemyStats : MonoBehaviour
{
    [Header("Enemy Stats")]
    public float maxHp = 100f;       
    public float currentHp;   
    
    // ★ [복구됨] 이 변수가 없어서 에러가 났었습니다!
    public float attackDamage = 10f; 
    
    public float expReward = 500f;   

    [Header("HP Bar UI")]
    public Image hpBarFill;     
    public GameObject hpCanvas; 

    [Header("Boss UI")]
    public Image bossScreenHPBar;    
    public GameObject bossUIFrame;   

    // 두 종류의 보스 스크립트 연결
    private BossAntlion antlionScript;
    private BossMantis mantisScript; 

    void Start()
    {
        currentHp = maxHp;
        
        antlionScript = GetComponent<BossAntlion>(); 
        mantisScript = GetComponent<BossMantis>();

        // 보스 UI 프레임 초기화
        if (bossUIFrame != null) bossUIFrame.SetActive(false);
        
        // 보스라면 일반 몬스터용 머리 위 체력바는 끄기
        if (antlionScript != null || mantisScript != null)
        {
            if (hpCanvas != null) hpCanvas.SetActive(false); 
        }

        UpdateHPBar(); 
    }

    public void ShowBossUI()
    {
        if (bossUIFrame != null) bossUIFrame.SetActive(true);
        else if (bossScreenHPBar != null) bossScreenHPBar.gameObject.SetActive(true);
    }

    public void TakeDamage(float damage)
    {
        if (currentHp <= 0) return;

        currentHp -= damage;
        UpdateHPBar();

        // 연결된 보스가 있다면 피격 반응(OnHit) 호출
        if (antlionScript != null) antlionScript.OnHit();
        if (mantisScript != null) mantisScript.OnHit();

        if (currentHp <= 0)
        {
            Die();
        }
    }

    void UpdateHPBar()
    {
        float fillAmount = (maxHp > 0) ? currentHp / maxHp : 0;

        if (bossScreenHPBar != null) bossScreenHPBar.fillAmount = fillAmount;
        else if (hpBarFill != null) hpBarFill.fillAmount = fillAmount;
    }

    void Die()
    {
        // 플레이어에게 경험치 지급
        PlayerStats player = FindObjectOfType<PlayerStats>();
        if (player != null) player.GainExp(expReward);

        // UI 끄기
        if (bossUIFrame != null) bossUIFrame.SetActive(false);
        if (hpCanvas != null) hpCanvas.SetActive(false);

        // 보스별 사망 연출 호출
        if (antlionScript != null) antlionScript.StartDeathSequence();
        else if (mantisScript != null) mantisScript.StartDeathSequence();
        else Destroy(gameObject); // 일반 몬스터는 그냥 삭제
    }
}