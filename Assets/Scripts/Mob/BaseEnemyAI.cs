using UnityEngine;

public class BaseEnemyAI : MonoBehaviour
{
    [Header("Base Settings")]
    public float detectRange = 8f;
    public float moveSpeed = 3f;
    public float chaseSpeed = 5f;
    public float attackRange = 1.5f;

    [Header("Patrol Settings")]
    public float patrolDistance = 5f; // 좌우 정찰 범위
    protected Vector2 startPos;
    protected int patrolDir = 1;

    protected Transform player;
    protected Rigidbody2D rb;
    protected SpriteRenderer sr;
    protected Animator anim;
    protected EnemyStats stats; //
    protected bool isFacingRight = true;
    protected bool isAttacking = false;
    
    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        stats = GetComponent<EnemyStats>(); //
    }

    protected virtual void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        startPos = transform.position; // 시작 위치 기억
    }

    protected virtual void Update() { }

    protected void LookAt(float targetX)
    {
        float xDir = targetX - transform.position.x;
        if (xDir > 0 && !isFacingRight) Flip();
        else if (xDir < 0 && isFacingRight) Flip();
    }

    protected virtual void Flip()
    {
        isFacingRight = !isFacingRight;
        transform.eulerAngles = isFacingRight ? new Vector3(0, 0, 0) : new Vector3(0, 180, 0);
    }
}