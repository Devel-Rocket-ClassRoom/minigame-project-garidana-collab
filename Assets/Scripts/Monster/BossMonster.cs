using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class BossMonster : MonoBehaviour, IDamageable
{
    public MonsterData data;

    public event System.Action<float> OnHpChanged;

    private static readonly int ParamSpawn = Animator.StringToHash("Spawn");
    private static readonly int ParamMove = Animator.StringToHash("isMoving");
    private static readonly int ParamTakeDamage = Animator.StringToHash("TakeDamage");
    private static readonly int ParamDeath = Animator.StringToHash("Death");
    private static readonly int ParamGroundAttack = Animator.StringToHash("GroundAttack");
    private static readonly int ParamXAttack = Animator.StringToHash("XAttack");
    private static readonly int ParamJumpAttack = Animator.StringToHash("JumpAttack");
    private static readonly int ParamSummon = Animator.StringToHash("Summon");

    [Header("Pattern")]
    [SerializeField] private float patternDelay = 1.5f;
    [SerializeField] private float lookAtPlayerSpeed = 720f;

    [Header("Attack")]
    [SerializeField] private float groundAttackRange = 3f;
    [SerializeField] private float xAttackRange = 6f;
    [SerializeField] private float xAttackAngle = 90f;
    [SerializeField] private float jumpAttackRadius = 4f;

    [Header("Phase Two")]
    [SerializeField] private float phaseTwoAttackMultiplier = 1.5f;
    [SerializeField] private float phaseTwoRangeMultiplier = 1.2f;
    [SerializeField] private float phaseTwoJumpRadiusMultiplier = 1.25f;
    [SerializeField] private float phaseTwoPatternDelayMultiplier = 0.75f;
    [SerializeField] private float phaseTwoXAttackAngleBonus = 15f;

    [Header("Summon")]
    [SerializeField] private GameObject[] minionPrefabs;
    [SerializeField] private Transform[] summonPoints;
    [SerializeField] private int summonMinCount = 10;
    [SerializeField] private int summonMaxCount = 15;

    [Header("Hit Effect")]
    [SerializeField] private ParticleSystem hitEffectPrefab;
    [SerializeField] private Transform hitEffectPoint;
    [SerializeField] private Color hitEffectColor = Color.white;
    [SerializeField] private float hitEffectScaleMultiplier = 1f;

    private NavMeshAgent agent;
    private Animator animator;
    private Transform player;
    private PlayerStats playerStats;

    private float currentHp;
    private float attackMultiplier = 1f;

    private bool isDead;
    private bool combatStarted;
    private bool patternRunning;
    private bool phaseTwoStarted;
    private bool summonReserved;
    private bool spawnStarted;
    private bool hasLastAttackPattern;
    private BossAttackType lastAttackPattern;

    public bool IsDead => isDead;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (data == null)
        {
            Debug.LogError($"{name} has no MonsterData assigned.", this);
            enabled = false;
            return;
        }

        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject == null)
        {
            Debug.LogError($"{name} could not find Player by tag.", this);
            enabled = false;
            return;
        }

        player = playerObject.transform;
        playerStats = player.GetComponent<PlayerStats>();

        currentHp = data.maxHp;

        if (agent != null)
        {
            agent.speed = data.moveSpeed;
            agent.angularSpeed = lookAtPlayerSpeed;
        }
    }

    private void Update()
    {
        if (isDead || !combatStarted || patternRunning || (playerStats != null && playerStats.IsDead))
        {
            return;
        }

        FacePlayer();

        StartCoroutine(PatternRoutine());
    }

    private void FacePlayer()
    {
        if (player == null)
        {
            return;
        }

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            lookAtPlayerSpeed * Time.deltaTime
        );
    }

    public void BeginSpawn()
    {
        if (isDead || spawnStarted)
        {
            return;
        }

        spawnStarted = true;
        combatStarted = false;
        patternRunning = false;

        if (agent != null)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }

        animator.SetBool(ParamMove, false);
        animator.SetTrigger(ParamSpawn);
    }

    private IEnumerator PatternRoutine()
    {
        patternRunning = true;

        if (agent != null)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }

        animator.SetBool(ParamMove, false);

        yield return new WaitForSeconds(CurrentPatternDelay);

        if (summonReserved)
        {
            summonReserved = false;
            yield return StartCoroutine(SummonPattern());
        }
        else
        {
            BossAttackType nextPattern = GetNextAttackPattern();

            switch (nextPattern)
            {
                case BossAttackType.GroundAttack:
                    yield return StartCoroutine(GroundAttackPattern());
                    break;

                case BossAttackType.XAttack:
                    yield return StartCoroutine(XAttackPattern());
                    break;

                case BossAttackType.JumpAttack:
                    yield return StartCoroutine(JumpAttackPattern());
                    break;
            }
        }

        patternRunning = false;
    }

    private BossAttackType GetNextAttackPattern()
    {
        if (player == null)
        {
            return BossAttackType.GroundAttack;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= CurrentGroundAttackRange)
        {
            return ChoosePattern(BossAttackType.GroundAttack, BossAttackType.XAttack);
        }

        if (distance <= CurrentXAttackRange)
        {
            return ChoosePattern(BossAttackType.XAttack, BossAttackType.JumpAttack);
        }

        return ChoosePattern(BossAttackType.JumpAttack);
    }

    private BossAttackType ChoosePattern(BossAttackType primary, BossAttackType? secondary = null)
    {
        BossAttackType selected = primary;

        if (secondary.HasValue)
        {
            selected = Random.value < 0.5f ? primary : secondary.Value;

            if (hasLastAttackPattern && selected == lastAttackPattern)
            {
                selected = selected == primary ? secondary.Value : primary;
            }
        }

        lastAttackPattern = selected;
        hasLastAttackPattern = true;
        return selected;
    }

    private IEnumerator GroundAttackPattern()
    {
        animator.SetTrigger(ParamGroundAttack);
        yield return WaitForAnimationEventOrFallback();
    }

    private IEnumerator XAttackPattern()
    {
        animator.SetTrigger(ParamXAttack);
        yield return WaitForAnimationEventOrFallback();
    }

    private IEnumerator JumpAttackPattern()
    {
        animator.SetTrigger(ParamJumpAttack);
        yield return WaitForAnimationEventOrFallback();
    }

    private IEnumerator SummonPattern()
    {
        animator.SetTrigger(ParamSummon);
        yield return WaitForAnimationEventOrFallback();
    }

    private IEnumerator WaitForAnimationEventOrFallback()
    {
        // 1차 구현에서는 애니메이션 이벤트 연결 전에도 패턴이 멈추지 않게 임시 대기시간 사용
        yield return new WaitForSeconds(2f);
    }

    public void OnSpawnFinished()
    {
        combatStarted = true;

        if (agent != null && agent.enabled)
        {
            agent.isStopped = false;
        }
    }

    public void OnGroundAttackHit()
    {
        if (isDead || player == null || (playerStats != null && playerStats.IsDead))
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= CurrentGroundAttackRange)
        {
            playerStats?.TakeDamage(data.attackDamage * attackMultiplier);
        }
    }

    public void OnXAttackHit()
    {
        if (isDead || player == null || (playerStats != null && playerStats.IsDead))
        {
            return;
        }

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;

        float distance = toPlayer.magnitude;
        float angle = Vector3.Angle(transform.forward, toPlayer.normalized);

        if (distance <= CurrentXAttackRange && angle <= CurrentXAttackAngle * 0.5f)
        {
            playerStats?.TakeDamage(data.attackDamage * attackMultiplier);
        }
    }

    public void OnJumpAttackLand()
    {
        if (isDead || (playerStats != null && playerStats.IsDead))
        {
            return;
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, CurrentJumpAttackRadius);

        foreach (Collider hit in hits)
        {
            PlayerStats playerStats = hit.GetComponentInParent<PlayerStats>();
            if (playerStats != null && !playerStats.IsDead)
            {
                playerStats.TakeDamage(data.attackDamage * attackMultiplier);
                break;
            }
        }
    }

    public void OnSummonMinions()
    {
        if (minionPrefabs == null || minionPrefabs.Length == 0)
        {
            return;
        }

        if (summonPoints == null || summonPoints.Length == 0)
        {
            return;
        }

        int count = Random.Range(summonMinCount, summonMaxCount + 1);

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = minionPrefabs[Random.Range(0, minionPrefabs.Length)];
            Transform point = summonPoints[Random.Range(0, summonPoints.Length)];

            Instantiate(prefab, point.position, point.rotation);
        }
    }

    public void TakeDamage(float damage)
    {
        TakeHit(DamageHitInfo.FromDamage(damage));
    }

    public void TakeHit(DamageHitInfo hitInfo)
    {
        if (isDead)
        {
            return;
        }

        float damage = hitInfo.Damage;
        currentHp -= damage;
        OnHpChanged?.Invoke(currentHp / data.maxHp);

        PlayHitEffect(hitInfo);

        animator.SetTrigger(ParamTakeDamage);

        if (!phaseTwoStarted && currentHp <= data.maxHp * 0.5f)
        {
            EnterPhaseTwo();
        }

        if (currentHp <= 0)
        {
            Die();
        }
    }

    private void PlayHitEffect(DamageHitInfo hitInfo)
    {
        ParticleSystem effectPrefab = hitInfo.AttackStage != null && hitInfo.AttackStage.HitEffectPrefab != null
            ? hitInfo.AttackStage.HitEffectPrefab
            : hitEffectPrefab;

        if (effectPrefab == null)
        {
            return;
        }

        Vector3 spawnPosition = hitInfo.HasHitPoint
            ? hitInfo.HitPoint
            : hitEffectPoint != null
                ? hitEffectPoint.position
                : transform.position + Vector3.up;

        Quaternion spawnRotation = hitInfo.HitDirection.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(hitInfo.HitDirection.normalized)
            : transform.rotation;

        ParticleSystem effect = Instantiate(
            effectPrefab,
            spawnPosition,
            spawnRotation
        );

        ParticleSystem.MainModule main = effect.main;
        main.startColor = hitInfo.AttackStage != null
            ? hitInfo.AttackStage.HitEffectColor
            : hitEffectColor;

        float scaleMultiplier = hitEffectScaleMultiplier;
        if (hitInfo.AttackStage != null)
        {
            scaleMultiplier *= hitInfo.AttackStage.HitEffectScaleMultiplier;
        }

        effect.transform.localScale *= Mathf.Max(0.1f, scaleMultiplier);
        effect.Play();

        float lifetime = main.duration + main.startLifetime.constantMax;
        Destroy(effect.gameObject, Mathf.Max(0.1f, lifetime));
    }

    private void Die()
    {
        isDead = true;
        combatStarted = false;
        patternRunning = false;

        if (agent != null)
        {
            agent.enabled = false;
        }

        animator.SetTrigger(ParamDeath);

        foreach (Collider collider in GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }


        if (!string.IsNullOrEmpty(data.questTargetId))
        {
            QuestManager.Instance?.ReportKill(data.questTargetId);
        }

        ProcessDrop();

        Destroy(gameObject, 5f);
    }

    private void ProcessDrop()
    {
        int gold = Random.Range(data.goldMin, data.goldMax + 1);

        PlayerStats playerStats = player.GetComponent<PlayerStats>();
        if (playerStats == null)
        {
            return;
        }

        playerStats.AddGold(gold);
        playerStats.AddExp(data.expReward);
    }

    private void EnterPhaseTwo()
    {
        phaseTwoStarted = true;
        summonReserved = true;
        attackMultiplier = phaseTwoAttackMultiplier;
    }

    private float CurrentGroundAttackRange => groundAttackRange * CurrentRangeMultiplier;
    private float CurrentXAttackRange => xAttackRange * CurrentRangeMultiplier;
    private float CurrentXAttackAngle => xAttackAngle + (phaseTwoStarted ? phaseTwoXAttackAngleBonus : 0f);
    private float CurrentJumpAttackRadius => jumpAttackRadius * (phaseTwoStarted ? phaseTwoJumpRadiusMultiplier : 1f);
    private float CurrentPatternDelay => patternDelay * (phaseTwoStarted ? phaseTwoPatternDelayMultiplier : 1f);
    private float CurrentRangeMultiplier => phaseTwoStarted ? phaseTwoRangeMultiplier : 1f;
}

public enum BossAttackType
{
    GroundAttack,
    XAttack,
    JumpAttack
}
