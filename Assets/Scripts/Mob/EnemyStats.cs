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

    // ★ [수정] private -> public으로 바꿔서 Inspector에서 보이게 함
    [Header("Boss UI (직접 드래그해서 넣으세요)")]
    public Image bossScreenHPBar; 

    private BossAntlion bossScript;

    void Start()
    {
        currentHp = maxHp;
        bossScript = GetComponent<BossAntlion>(); 

        // ★ [수정] 코드로 찾는 기능 삭제 -> 이미 Inspector에서 넣었을 테니까!
        
        // 만약 보스 HP바가 연결되어 있다면?
        if (bossScreenHPBar != null)
        {
            // 1. 꺼져있을 수도 있으니 강제로 켠다! (부모인 프레임까지 켜주면 더 좋음)
            bossScreenHPBar.gameObject.SetActive(true);
            
            // (팁: 만약 프레임 전체를 껐다 켰다 하려면 프레임 오브젝트도 변수로 받아서 켜야 함)
            // 일단은 바라도 켜지게 설정.
            if(bossScreenHPBar.transform.parent != null)
                bossScreenHPBar.transform.parent.gameObject.SetActive(true);
                
            // 2. 일반 몬스터용 HP바는 끈다.
            if (hpCanvas != null) hpCanvas.SetActive(false);
        }

        UpdateHPBar(); 
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
        
        // ★ [추가] 보스가 죽으면 화면 HP바 끄기
        if (bossScreenHPBar != null)
        {
             if(bossScreenHPBar.transform.parent != null)
                bossScreenHPBar.transform.parent.gameObject.SetActive(false);
             else
                bossScreenHPBar.gameObject.SetActive(false);
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