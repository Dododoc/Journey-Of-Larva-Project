using UnityEngine;

public class FluidTrap : MonoBehaviour
{
    [Header("Settings")]
    public float duration = 5.0f;       
    public float slowMultiplier = 0.4f; 
    public SpriteRenderer spriteRenderer; 

    void Start()
    {
        if(spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        Destroy(gameObject, duration); 
    }

    void Update()
    {
        if (spriteRenderer != null)
        {
            float alpha = Mathf.MoveTowards(spriteRenderer.color.a, 0f, Time.deltaTime / duration);
            Color newColor = spriteRenderer.color;
            newColor.a = alpha;
            spriteRenderer.color = newColor;
        }
    }

    void OnTriggerEnter2D(Collider2D other) { ApplyDebuff(other); }
    void OnTriggerStay2D(Collider2D other) { ApplyDebuff(other); }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerStats pStats = other.GetComponent<PlayerStats>();
            if (pStats != null) pStats.SetDebuff(false, 1.0f); 
        }
    }

    void ApplyDebuff(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerStats pStats = other.GetComponent<PlayerStats>();
            if (pStats != null) pStats.SetDebuff(true, slowMultiplier); 
        }
    }
}