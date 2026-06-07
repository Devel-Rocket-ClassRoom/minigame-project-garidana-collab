using UnityEngine;

public class PlayerRewardFloatingText : MonoBehaviour
{
    [SerializeField]
    private PlayerStats playerStats;

    [SerializeField]
    private PlayerHealing playerHealing;

    [SerializeField]
    private FloatingTextEffect floatingTextPrefab;

    [SerializeField]
    private Transform floatingTextPoint;

    [SerializeField]
    private ParticleSystem healEffectPrefab;

    [SerializeField]
    private ParticleSystem levelUpEffectPrefab;

    [SerializeField]
    private Transform effectSpawnPoint;

    [SerializeField, Range(0f, 1f)]
    private float lowHealthThreshold = 0.2f;

    [SerializeField, Min(0.1f)]
    private float lowHealthWarningInterval = 1f;

    private float nextLowHealthWarningTime;

    private void Awake()
    {
        if (playerStats == null)
        {
            playerStats = GetComponent<PlayerStats>();
        }

        if (playerHealing == null)
        {
            playerHealing = GetComponent<PlayerHealing>();
        }
    }

    private void Update()
    {
        if (playerStats == null || playerStats.IsDead || playerStats.MaxHealth <= 0f)
        {
            nextLowHealthWarningTime = 0f;
            return;
        }

        float healthRatio = playerStats.CurrentHealth / playerStats.MaxHealth;
        if (healthRatio > lowHealthThreshold)
        {
            nextLowHealthWarningTime = 0f;
            return;
        }

        if (Time.time < nextLowHealthWarningTime)
        {
            return;
        }

        SpawnFloatingText("체력 낮음", Color.red, Vector3.up * 0.2f);
        nextLowHealthWarningTime = Time.time + lowHealthWarningInterval;
    }

    private void OnEnable()
    {
        if (playerStats != null)
        {
            playerStats.ExpGained += ShowExp;
            playerStats.DamageTaken += ShowDamage;
            playerStats.LevelChanged += ShowLevelUp;
        }

        if (playerHealing != null)
        {
            playerHealing.PotionHealed += ShowHeal;
        }
    }

    private void OnDisable()
    {
        if (playerStats != null)
        {
            playerStats.ExpGained -= ShowExp;
            playerStats.DamageTaken -= ShowDamage;
            playerStats.LevelChanged -= ShowLevelUp;
        }

        if (playerHealing != null)
        {
            playerHealing.PotionHealed -= ShowHeal;
        }
    }

    private void ShowExp(int amount)
    {
        if (floatingTextPrefab == null)
        {
            return;
        }

        Vector3 position = floatingTextPoint != null
            ? floatingTextPoint.position
            : transform.position + Vector3.up * 2f;

        FloatingTextEffect effect = Instantiate(
            floatingTextPrefab,
            position,
            Quaternion.identity
        );

        effect.Initialize($"+{amount} EXP", Color.green);
    }

    private void ShowHeal(int amount)
    {
        SpawnFloatingText($"+{amount} HP", new Color(0.2f, 1f, 0.45f), Vector3.left * 0.25f);
        SpawnEffect(healEffectPrefab);
    }

    private void ShowLevelUp(int level)
    {
        SpawnFloatingText($"LEVEL UP! Lv.{level}", new Color(1f, 0.82f, 0.2f), Vector3.up * 0.35f);
        SpawnEffect(levelUpEffectPrefab);
    }

    private void ShowDamage(int amount)
    {
        SpawnFloatingText($"-{amount} HP", Color.red, Vector3.right * 0.25f);
    }

    public void ShowNotice(string value, Color color, float fontSize = -1f)
    {
        SpawnFloatingText(value, color, Vector3.up * 0.2f, fontSize);
    }

    private void SpawnFloatingText(string value, Color color, Vector3 offset, float fontSize = -1f)
    {
        if (floatingTextPrefab == null)
        {
            return;
        }

        Vector3 position = floatingTextPoint != null
            ? floatingTextPoint.position + offset
            : transform.position + Vector3.up * 2f + offset;

        FloatingTextEffect effect = Instantiate(
            floatingTextPrefab,
            position,
            Quaternion.identity
        );

        effect.Initialize(value, color, fontSize);
    }

    private void SpawnEffect(ParticleSystem effectPrefab)
    {
        if (effectPrefab == null)
        {
            return;
        }

        Transform spawnPoint = effectSpawnPoint != null ? effectSpawnPoint : transform;
        ParticleSystem effect = Instantiate(
            effectPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        ParticleSystem.MainModule main = effect.main;
        effect.Play();
        Destroy(effect.gameObject, main.duration + main.startLifetime.constantMax);
    }
}
