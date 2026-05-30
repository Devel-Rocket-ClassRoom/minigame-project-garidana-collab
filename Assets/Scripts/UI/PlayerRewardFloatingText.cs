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

    private void SpawnFloatingText(string value, Color color, Vector3 offset)
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

        effect.Initialize(value, color);
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
