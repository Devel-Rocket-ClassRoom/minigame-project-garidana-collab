using System.Collections;
using UnityEngine;
using UnityEngine.AI;


public class BaseMonster : MonoBehaviour, IDamageable
{
    // 몬스터 데이터 Scriptable Object 가져옴
    public MonsterData data;

    // 몬스터 머리위 체력 바 연결 해줄 이벤트 
    public event System.Action<float> OnHpChanged;

    // 몬스터 애니메이션 연결 파라미터
    static readonly int ParamMove = Animator.StringToHash("isMoving"); 
    static readonly int ParamAttack = Animator.StringToHash("Attack");
    static readonly int ParamTakeDamage = Animator.StringToHash("TakeDamage");
    static readonly int ParamDeath = Animator.StringToHash("Death");   
    static readonly int ParamIsDead = Animator.StringToHash("isDead"); 

    // 참조 요소 (NavMeshAgent, 애니메이터, 플레이어 위치정보)
    protected NavMeshAgent agent;
    protected Animator animator;
    protected Transform player;

    private float currentHp;
    private bool isDead;
    private float lastAttackTime;

    // 배회 시작 및 타겟 지점
    private Vector3 patrolStartPos;
    private Vector3 patrolTargetPos;
    private bool patrolInitialized;
    private bool patrolGoingForward = true;
    private float nextPatrolTime;

    [SerializeField]
    private float patrolDistance = 3f;

    [Header("Hit Reaction")]
    [SerializeField]
    private float knockbackDistance = 0.35f;

    [SerializeField]
    private float knockbackDuration = 0.8f;

    private bool isKnockbacking;
    private Coroutine knockbackCoroutine;

    public bool IsDead => isDead;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        player = GameObject.FindWithTag("Player").transform;

        currentHp = data.maxHp;
        agent.speed = data.moveSpeed;

        patrolStartPos = transform.position;
    }

    private void Update()
    {
        if (isDead || isKnockbacking) return;

        float dist = Vector3.Distance(transform.position, player.position);

        // 공격 범위 내 들어올 시 공격 (TryAttack)
        if (dist <= data.attackRange)
        {
            TryAttack();
        }
        // 감지 범위 내 들어올 시 추격 (ChasePlayer)
        else if (dist <= data.detectionRange)
        {
            ChasePlayer();
        }
        // 평상시엔 정해진 구역 배회
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

    // 배회 
    private void Patrol()
    {
        if (!patrolInitialized)
        {
            patrolInitialized = true;
            patrolStartPos = transform.position;
            patrolTargetPos = patrolStartPos + transform.forward * patrolDistance;
            nextPatrolTime = Time.time + Random.Range(1f, 10f);
        }

        if (Time.time < nextPatrolTime)
        {
            animator.SetBool(ParamMove, false);
            return;
        }

        Vector3 target = patrolGoingForward ? patrolTargetPos : patrolStartPos;

        agent.SetDestination(target);

        animator.SetBool(ParamMove, true);

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            patrolGoingForward = !patrolGoingForward;
            nextPatrolTime = Time.time + Random.Range(1f, 10f);

            agent.ResetPath();
            animator.SetBool(ParamMove, false);
        }
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

    // 실제 몬스터 공격. 애니메이션 이벤트에 연결해서 모션과 타격 시점 연동
    public void OnAttackHit()
    {
        if (isDead || isKnockbacking) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= data.attackRange)
        {
            player.GetComponent<PlayerStats>().TakeDamage(data.attackDamage);
        }
    }

    // IDamageable에서 상속받은 피격 함수
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        currentHp -= damage;
        animator.SetTrigger(ParamTakeDamage);

        OnHpChanged?.Invoke(currentHp / data.maxHp);

        if (currentHp <= 0)
        {
            Die();
            return;
        }

        StartKnockback();
    }

    private void StartKnockback()
    {
        if (knockbackCoroutine != null)
        {
            StopCoroutine(knockbackCoroutine);
        }

        Vector3 direction = transform.position - player.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            direction = -transform.forward;
        }

        knockbackCoroutine = StartCoroutine(KnockbackRoutine(direction.normalized));
    }

    private IEnumerator KnockbackRoutine(Vector3 direction)
    {
        isKnockbacking = true;

        bool shouldRestoreAgent = agent != null && agent.enabled;

        if (shouldRestoreAgent)
        {
            agent.ResetPath();
            agent.isStopped = true;
            animator.SetBool(ParamMove, false);
        }

        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, knockbackDuration);
        float speed = knockbackDistance / safeDuration;

        while (!isDead && elapsed < safeDuration)
        {
            float delta = Time.unscaledDeltaTime;
            elapsed += delta;

            Vector3 movement = direction * (speed * delta);
            if (agent != null && agent.enabled)
            {
                agent.Move(movement);
            }
            else
            {
                transform.position += movement;
            }

            yield return null;
        }

        if (!isDead && shouldRestoreAgent && agent != null && agent.enabled)
        {
            agent.isStopped = false;
        }

        isKnockbacking = false;
        knockbackCoroutine = null;
    }

    private void Die()
    {
        isDead = true;
        isKnockbacking = false;
        agent.enabled = false;
        //animator.SetBool("isDead", true);
        animator.SetTrigger(ParamIsDead);
        animator.SetTrigger(ParamDeath);

        // 몬스터 사망시 콜라이더 끄기
        foreach (var collider in GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }

        if (!string.IsNullOrEmpty(data.questTargetId))
        {
            QuestManager.Instance?.ReportKill(data.questTargetId);
        }

        ProcessDrop();

        StartCoroutine(SinkAndDestroy());
        // 지금은 몬스터 사망 자연스럽게 땅속으로 사라지게 하기 위함
        // 나중엔 Destroy 다시 살려서 이펙트추가 할 예정
        //Destroy(gameObject, 3f);
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

    private IEnumerator SinkAndDestroy()
    {
        yield return new WaitForSeconds(1f);

        float sinkDuration = 2f;
        float elapsed = 0f;

        Vector3 startPosition = transform.position;
        Vector3 endPosition = startPosition + Vector3.down * 1.5f;

        while (elapsed < sinkDuration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPosition, endPosition, elapsed / sinkDuration);
            yield return null;
        }

            Destroy(gameObject);
        }
}
