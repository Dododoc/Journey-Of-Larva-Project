using UnityEngine;
using UnityEngine.UI;

public class EnemyStats : MonoBehaviour
{
    [Header("Enemy Stats")]
    public float maxHp = 30f;       
    public float currentHp;         
    public float attackDamage = 10f; 
    public float expReward = 50f;   

    [Header("HP Bar UI (일반 몬스터용)")]
    public Image hpBarFill;     
    public GameObject hpCanvas; 

    [Header("Boss UI (직접 드래그)")]
    public Image bossScreenHPBar;    // 빨간색 게이지 (RedBarFill)
    public GameObject bossUIFrame;   // ★ [추가] 보스 체력바 전체 틀 (BossFrame)

    private BossAntlion bossScript;

    void Start()
    {
        currentHp = maxHp;
        bossScript = GetComponent<BossAntlion>(); 

        // 시작할 때 보스 UI 전체(틀 포함) 끄기
        if (bossUIFrame != null)
        {
            bossUIFrame.SetActive(false);
        }
        else if (bossScreenHPBar != null)
        {
            // 혹시 틀을 연결 안 했으면 부모라도 끔
            if(bossScreenHPBar.transform.parent != null)
                bossScreenHPBar.transform.parent.gameObject.SetActive(false);
        }

        // 보스라면 일반 HP바 끄기
        if (bossScript != null && hpCanvas != null) 
            hpCanvas.SetActive(false);

        UpdateHPBar(); 
    }

    // 보스 등장 시 호출됨
    public void ShowBossUI()
    {
        // ★ 틀 전체를 켭니다
        if (bossUIFrame != null)
        {
            bossUIFrame.SetActive(true);
        }
        else if (bossScreenHPBar != null)
        {
            bossScreenHPBar.gameObject.SetActive(true);
        }
    }

    public void TakeDamage(float damage)
    {
        if (currentHp <= 0) return;

        currentHp -= damage;
        UpdateHPBar();

        if (bossScript != null) bossScript.OnHit();
        if (currentHp <= 0)
        {
            Die();
        }
    }

    void UpdateHPBar()
    {
        float fillAmount = (maxHp > 0) ? currentHp / maxHp : 0;

        if (bossScreenHPBar != null)
        {
            bossScreenHPBar.fillAmount = fillAmount;
        }
        else if (hpBarFill != null)
        {
            hpBarFill.fillAmount = fillAmount;
        }
    }

    void Die()
    {
        Debug.Log($"적 사망! 경험치 {expReward} 획득");
        
        PlayerStats player = FindObjectOfType<PlayerStats>();
        if (player != null) player.GainExp(expReward);

        if (hpCanvas != null) hpCanvas.SetActive(false);
        
        // ★ 보스 사망 시 틀 전체 끄기
        if (bossUIFrame != null)
        {
            bossUIFrame.SetActive(false);
        }
        else if (bossScreenHPBar != null && bossScreenHPBar.transform.parent != null)
        {
            bossScreenHPBar.transform.parent.gameObject.SetActive(false);
        }

        if (bossScript != null) bossScript.StartDeathSequence();
        else Destroy(gameObject); 
    }

    void LateUpdate() 
    {
        if (hpCanvas != null && hpCanvas.activeSelf)
        {
            hpCanvas.transform.rotation = Quaternion.identity;
        }
    }
}