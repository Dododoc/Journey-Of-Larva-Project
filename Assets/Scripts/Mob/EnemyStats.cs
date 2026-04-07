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

    [Header("Drop Item")]
    public GameObject dropItemPrefab; 

    private BossAntlion antlionScript;
    private BossMantis mantisScript; 
    private ScarecrowAI scarecrowScript; // ★ [추가] 허수아비 스크립트 연결용
    private SpriteRenderer sr;
    private Color originalColor;

    void Start()
    {
        currentHp = maxHp;
        antlionScript = GetComponent<BossAntlion>(); 
        mantisScript = GetComponent<BossMantis>();
        scarecrowScript = GetComponent<ScarecrowAI>(); // ★ [추가] 허수아비 컴포넌트 찾기

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

        if (GameManager.instance != null)
        {
            GameManager.instance.killCount++;
        }

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
        else if (scarecrowScript != null) scarecrowScript.StartDeathSequence(); // ★ [추가] 허수아비 전용 죽음 연출 실행
        else 
        {
            if (dropItemPrefab != null)
            {
                Instantiate(dropItemPrefab, transform.position, Quaternion.identity);
            }
            Destroy(gameObject); 
        }
    }
}