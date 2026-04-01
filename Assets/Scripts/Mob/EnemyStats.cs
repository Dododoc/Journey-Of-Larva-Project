using UnityEngine;
using UnityEngine.UI;
using System.Collections; 

public class EnemyStats : MonoBehaviour
{
    [Header("Enemy Stats")]
    public float maxHp = 100f;       
    public float currentHp;   
    public float attackDamage = 10f; 
    public float expReward = 500f;   

    [Header("HP Bar UI")]
    public Image hpBarFill;     
    public GameObject hpCanvas; 

    [Header("Boss UI")]
    public Image bossScreenHPBar;    
    public GameObject bossUIFrame;   

    // ★ [새로 추가] 죽었을 때 떨어뜨릴 아이템 프리팹
    [Header("Drop Item")]
    public GameObject dropItemPrefab; 

    private BossAntlion antlionScript;
    private BossMantis mantisScript; 
    private SpriteRenderer sr;
    private Color originalColor;

    void Start()
    {
        currentHp = maxHp;
        antlionScript = GetComponent<BossAntlion>(); 
        mantisScript = GetComponent<BossMantis>();

        if (bossUIFrame != null) bossUIFrame.SetActive(false);
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
    
    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) originalColor = sr.color;
    }

    public void TakeDamage(float damage)
    {
        if (currentHp <= 0) return;

        currentHp -= damage;
        UpdateHPBar();
        StopCoroutine("HitFlashRoutine");
        StartCoroutine("HitFlashRoutine");
        
        if (antlionScript != null) antlionScript.OnHit();
        if (mantisScript != null) mantisScript.OnHit();

        if (currentHp <= 0)
        {
            Die();
        }
    }
    
    IEnumerator HitFlashRoutine()
    {
        sr.color = Color.red; 
        yield return new WaitForSeconds(0.1f);
        sr.color = originalColor; 
    }

    void UpdateHPBar()
    {
        float fillAmount = (maxHp > 0) ? currentHp / maxHp : 0;
        if (bossScreenHPBar != null) bossScreenHPBar.fillAmount = fillAmount;
        else if (hpBarFill != null) hpBarFill.fillAmount = fillAmount;
    }

    void Die()
    {
        PlayerStats player = FindFirstObjectByType<PlayerStats>();
        if (player != null) player.GainExp(expReward);

        if (bossUIFrame != null) bossUIFrame.SetActive(false);
        if (hpCanvas != null) hpCanvas.SetActive(false);

        if (QuestManager.instance != null)
        {
            if (GetComponent<LadybugAI>() != null) QuestManager.instance.CompleteQuest(0);
            else if (GetComponent<CricketAI>() != null) QuestManager.instance.CompleteQuest(2);
            else if (GetComponent<ScorpionAI>() != null) QuestManager.instance.CompleteQuest(4);
        }

        if (antlionScript != null) antlionScript.StartDeathSequence();
        else if (mantisScript != null) mantisScript.StartDeathSequence();
        else 
        {
            // ===================================================
            // ★ [추가된 핵심 로직] 전갈이 죽을 때 아이템을 바닥에 소환합니다!
            // ===================================================
            if (dropItemPrefab != null)
            {
                // 전갈이 있던 자리에 아이템 생성
                Instantiate(dropItemPrefab, transform.position, Quaternion.identity);
            }
            
            Destroy(gameObject); // 일반 몬스터는 그냥 삭제
        }
    }
}