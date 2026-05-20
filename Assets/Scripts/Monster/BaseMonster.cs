using UnityEngine;
using UnityEngine.AI;


public class BaseMonster : MonoBehaviour, IDamageable
{
    public MonsterData data;

    public event System.Action<float> OnHpChanged;

    static readonly int ParamMove = Animator.StringToHash("isMoving"); 
    static readonly int ParamAttack = Animator.StringToHash("Attack");
    static readonly int ParamTakeDamage = Animator.StringToHash("TakeDamage");
    static readonly int ParamDeath = Animator.StringToHash("Death");    

    protected NavMeshAgent agent;
    protected Animator animator;
    protected Transform player;

    float currentHp;
    bool isDead;
    float lastAttackTime;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        player = GameObject.FindWithTag("Player").transform;

        currentHp = data.maxHp;
        agent.speed = data.moveSpeed;
    }

    private void Update()
    {
        if (isDead) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= data.attackRange)
        {
            TryAttack();
        }
        else if (dist <= data.detectionRange)
        {
            ChasePlayer();
        }
        else
        {
            Patrol();
        }
    }

    // 몬스터 이동 관련
    private void ChasePlayer()
    {
        agent.SetDestination(player.position);
        animator.SetBool(ParamMove, true);
    }

    private void Patrol()
    {
        agent.ResetPath();
        animator.SetBool(ParamMove, false);
    }

    // 몬스터 공격
    private void TryAttack()
    {
        agent.ResetPath();
        animator.SetBool(ParamMove, false);

        if (Time.time - lastAttackTime < data.attackCooldown) return;

        lastAttackTime = Time.time;

        animator.SetTrigger(ParamAttack);
    }

    public void OnAttackHit()
    {
        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= data.attackRange)
        {
            player.GetComponent<PlayerStats>().TakeDamage(data.attackDamage);
        }
    }


    public void TakeDamage(float damage)
    {
        if (isDead) return;
        currentHp -= damage;
        animator.SetTrigger(ParamTakeDamage);

        OnHpChanged?.Invoke(currentHp / data.maxHp);

        if (currentHp <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        agent.enabled = false;
        animator.SetTrigger(ParamDeath);

        ProcessDrop();

        Destroy(gameObject, 3f);
    }

    // 몬스터 골드 및 경험치 드롭
    private void ProcessDrop()
    {
        int gold = Random.Range(data.goldMin, data.goldMax + 1);

        PlayerStats playerStats = player.GetComponent<PlayerStats>();
        playerStats.AddGold(gold);
        playerStats.AddExp(data.expReward);
        
        foreach (var entry in data.questDrops)
        {
            if (Random.value <= entry.dropChance)
            {
                DropItem(entry.item);
            }
        }
    }

    // 몬스터 아이템 드롭 함수
    private void DropItem(ItemData item)
    {
        // 월드에 아이템 오브젝트를 스폰
        // item.worldPrefab은 ItemData에 등록된 아이템 프리팹
        //Instantiate(item.worldPrefab, transform.position, Vector3.up * 0.5f, Quaternion.identity);
    }
}
