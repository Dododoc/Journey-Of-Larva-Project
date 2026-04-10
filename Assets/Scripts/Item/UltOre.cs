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

    [Header("Unlock UI")]
    public GameObject unlockCanvas;  
    public float uiDisplayTime = 2.5f; // UI가 떠 있는 시간
    public float uiFadeTime = 1.0f;    // UI가 사라지는 데 걸리는 시간

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
        if (hpCanvas != null && !hpCanvas.activeSelf) hpCanvas.SetActive(true);

        currentHp -= damage;
        if (hpFillImage != null) hpFillImage.fillAmount = currentHp / maxHp;

        StartCoroutine(ShakeRoutine());
        if (currentHp <= 0) Die();
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
        if (anim != null) anim.SetTrigger("DoBreak"); 

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
                if (beetle.vSkillUI != null) beetle.vSkillUI.UnlockSkill(); 
            }
        }

        // ==========================================
        // ★ [UI 켜기]
        // ==========================================
        if (unlockCanvas != null)
        {
            unlockCanvas.SetActive(true);
            // CanvasGroup이 없으면 코드로 자동 추가해줍니다.
            if (unlockCanvas.GetComponent<CanvasGroup>() == null)
                unlockCanvas.AddComponent<CanvasGroup>();
        }

        // 보석 파편 페이드 아웃 (1초)
        float shardFadeTime = 1.0f;
        float shardTimer = 0f;
        while (shardTimer < shardFadeTime)
        {
            shardTimer += Time.deltaTime;
            float alpha = Mathf.Lerp(1, 0, shardTimer / shardFadeTime);
            foreach (SpriteRenderer sr in allRenderers)
            {
                if (sr != null) sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, alpha);
            }
            yield return null;
        }

        // 플레이어가 글자를 읽는 시간 대기
        yield return new WaitForSeconds(uiDisplayTime); 

        // ==========================================
        // ★ [핵심 추가] UI 페이드 아웃 (스르륵 사라짐)
        // ==========================================
        if (unlockCanvas != null)
        {
            CanvasGroup uiGroup = unlockCanvas.GetComponent<CanvasGroup>();
            float uiTimer = 0f;
            while (uiTimer < uiFadeTime)
            {
                uiTimer += Time.deltaTime;
                if (uiGroup != null)
                {
                    // 투명도를 1에서 0으로 서서히 줄입니다.
                    uiGroup.alpha = Mathf.Lerp(1, 0, uiTimer / uiFadeTime);
                }
                yield return null;
            }
        }

        Debug.Log("황금 광석 연출 종료! 오브젝트를 파괴합니다.");
        Destroy(gameObject); 
    }
}