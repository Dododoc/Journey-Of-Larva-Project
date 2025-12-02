using UnityEngine;
using UnityEngine.UI;

public class EnemyStats : MonoBehaviour
{
    [Header("Enemy Stats")]
    public float maxHp = 30f;       // 최대 체력
    public float currentHp;         // 현재 체력
    public float attackDamage = 10f; // 공격력
    public float expReward = 50f;   // ★ [추가] 이 몬스터를 잡으면 주는 경험치

    [Header("HP Bar UI")]
    public Image hpBarFill;     
    public GameObject hpCanvas; 

    void Start()
    {
        currentHp = maxHp;
        UpdateHPBar(); 
    }

    public void TakeDamage(float damage)
    {
        currentHp -= damage;
        // Debug.Log($"적 피격! 남은 체력: {currentHp}"); // 로그가 너무 많으면 주석 처리

        UpdateHPBar();

        if (currentHp <= 0)
        {
            Die();
        }
    }

    void UpdateHPBar()
    {
        if (hpBarFill != null)
        {
            if (maxHp > 0)
                hpBarFill.fillAmount = currentHp / maxHp;
            else
                hpBarFill.fillAmount = 0;
        }
    }

    void Die()
    {
        Debug.Log($"적 사망! 경험치 {expReward} 획득");
        
        // ★ [추가] 죽는 순간 플레이어를 찾아서 경험치를 줌
        // (Scene에 있는 PlayerStats를 가진 오브젝트를 찾음)
        PlayerStats player = FindObjectOfType<PlayerStats>();
        if (player != null)
        {
            player.GainExp(expReward);
        }

        // HP바 끄기
        if (hpCanvas != null) 
            hpCanvas.SetActive(false);

        // 적 삭제
        Destroy(gameObject); 
    }

    void LateUpdate() 
    {
        if (hpCanvas != null)
        {
            hpCanvas.transform.rotation = Quaternion.identity;
        }
    }
}