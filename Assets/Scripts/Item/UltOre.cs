using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class UltOre : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHp = 100f;
    private float currentHp;

    [Header("Floating Settings")]
    public float floatSpeed = 2f;    
    public float floatHeight = 0.2f; 
    private Vector3 startPos;

    [Header("UI Settings")]
    public GameObject hpCanvas;      
    public Image hpFillImage;        

    // ==========================================
    // ★ [추가됨] 직접 만드신 해금 UI 캔버스를 연결할 변수입니다!
    // ==========================================
    [Header("Unlock UI")]
    public GameObject unlockCanvas;  

    [Header("Effects & Unlock")]
    public GameObject goldAuraPrefab; 
    
    [Header("Aura Position & Timing")]
    public float auraOffsetY = -0.5f; 
    public float auraDuration = 1.5f; 

    private Animator anim; 
    private SpriteRenderer[] allRenderers; 
    private Collider2D[] allColliders;      
    private bool isDead = false;
    private bool isShaking = false;

    void Start()
    {
        currentHp = maxHp;
        startPos = transform.position;
        
        anim = GetComponent<Animator>(); 

        if (hpCanvas != null) hpCanvas.SetActive(false);
        
        // ★ 시작할 때 해금 UI 캔버스도 일단 숨겨둡니다.
        if (unlockCanvas != null) unlockCanvas.SetActive(false); 
    }

    void Update()
    {
        if (!isDead && !isShaking)
        {
            float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
            transform.position = new Vector3(startPos.x, newY, startPos.z);
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        if (hpCanvas != null && !hpCanvas.activeSelf)
        {
            hpCanvas.SetActive(true);
        }

        currentHp -= damage;
        if (hpFillImage != null) hpFillImage.fillAmount = currentHp / maxHp;

        StartCoroutine(ShakeRoutine());

        if (currentHp <= 0)
        {
            Die();
        }
    }

    IEnumerator ShakeRoutine()
    {
        isShaking = true;
        Vector3 originalPos = transform.position;

        float elapsed = 0.0f;
        while (elapsed < 0.2f)
        {
            float x = Random.Range(-0.1f, 0.1f);
            float y = Random.Range(-0.1f, 0.1f);
            transform.position = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPos;
        isShaking = false;
    }

    void Die()
    {
        isDead = true;
        
        if (hpCanvas != null) hpCanvas.SetActive(false);

        allRenderers = GetComponentsInChildren<SpriteRenderer>();
        allColliders = GetComponentsInChildren<Collider2D>();

        foreach (Collider2D col in allColliders)
        {
            if (col != null) col.enabled = false;
        }

        StartCoroutine(UnlockSequence());
    }

    IEnumerator UnlockSequence()
    {
        if (anim != null) 
        {
            anim.SetTrigger("DoBreak"); 
        }

        yield return new WaitForSeconds(2.0f);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && goldAuraPrefab != null)
        {
            Vector3 spawnPos = player.transform.position + new Vector3(0f, auraOffsetY, 0f);
            Instantiate(goldAuraPrefab, spawnPos, Quaternion.identity, player.transform);
        }

        Time.timeScale = 0.2f;
        yield return new WaitForSecondsRealtime(0.5f); 
        Time.timeScale = 1.0f;

        yield return new WaitForSeconds(auraDuration);

        if (player != null)
        {
            BeetleController beetle = player.GetComponent<BeetleController>();
            if (beetle != null)
            {
                beetle.isUltUnlocked = true; 
                if (beetle.vSkillUI != null) 
                {
                    beetle.vSkillUI.UnlockSkill(); 
                }
            }
        }

        // ==========================================
        // ★ [허수아비 방식] 캔버스를 짜잔! 하고 켭니다.
        // ==========================================
        if (unlockCanvas != null)
        {
            unlockCanvas.SetActive(true);
        }

        // 보석 파편 스프라이트들만 투명하게 페이드 아웃 시킵니다 (UI는 영향받지 않음)
        float fadeTime = 1.0f;
        float timer = 0f;
        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(1, 0, timer / fadeTime);

            foreach (SpriteRenderer sr in allRenderers)
            {
                if (sr != null) 
                {
                    sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, alpha);
                }
            }
            yield return null;
        }

        // ==========================================
        // ★ [매우 중요] UI 캔버스가 황금석의 자식이므로, 황금석이 파괴되면 UI도 같이 파괴됩니다.
        // 플레이어가 UI 글자를 넉넉히 읽을 수 있도록 파괴하기 전에 2.5초 정도 더 기다려줍니다!
        // ==========================================
        yield return new WaitForSeconds(2.5f); 

        Debug.Log("황금 광석 연출 종료! 오브젝트를 파괴합니다.");
        Destroy(gameObject); 
    }
}