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

    private float currentHp;
    private float attackMultiplier = 1f;

    private bool isDead;
    private bool combatStarted;
    private bool patternRunning;
    private bool phaseTwoStarted;
    private bool summonReserved;

    private int attackPatternIndex;

    public bool IsDead => isDead;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        player = GameObject.FindWithTag("Player").transform;

        currentHp = data.maxHp;

        if (agent != null)
        {
            agent.speed = data.moveSpeed;
            agent.angularSpeed = lookAtPlayerSpeed;
        }
    }

    private void Start()
    {
        animator.SetTrigger(ParamSpawn);
    }

    private void Update()
    {
        if (isDead || !combatStarted || patternRunning)
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

    private IEnumerator PatternRoutine()
    {
        patternRunning = true;

        if (agent != null)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }

        animator.SetBool(ParamMove, false);

        yield return new WaitForSeconds(patternDelay);

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
        BossAttackType pattern = attackPatternIndex switch
        {
            0 => BossAttackType.GroundAttack,
            1 => BossAttackType.XAttack,
            _ => BossAttackType.JumpAttack
        };

        attackPatternIndex = (attackPatternIndex + 1) % 3;
        return pattern;
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
    }

    public void OnGroundAttackHit()
    {
        if (isDead || player == null)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= groundAttackRange)
        {
            player.GetComponent<PlayerStats>()?.TakeDamage(data.attackDamage * attackMultiplier);
        }
    }

    public void OnXAttackHit()
    {
        if (isDead || player == null)
        {
            return;
        }

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;

        float distance = toPlayer.magnitude;
        float angle = Vector3.Angle(transform.forward, toPlayer.normalized);

        if (distance <= xAttackRange && angle <= xAttackAngle * 0.5f)
        {
            player.GetComponent<PlayerStats>()?.TakeDamage(data.attackDamage * attackMultiplier);
        }
    }

    public void OnJumpAttackLand()
    {
        if (isDead)
        {
            return;
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, jumpAttackRadius);

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                hit.GetComponent<PlayerStats>()?.TakeDamage(data.attackDamage * attackMultiplier);
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
            phaseTwoStarted = true;
            summonReserved = true;
            attackMultiplier = 1.5f;
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
        playerStats.AddGold(gold);
        playerStats.AddExp(data.expReward);
    }
}

public enum BossAttackType
{
    GroundAttack,
    XAttack,
    JumpAttack
}
