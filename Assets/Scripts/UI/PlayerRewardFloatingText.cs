using UnityEngine;

public class PlayerRewardFloatingText : MonoBehaviour
{
    [SerializeField]
    private PlayerStats playerStats;

    [SerializeField]
    private FloatingTextEffect floatingTextPrefab;

    [SerializeField]
    private Transform floatingTextPoint;

    private void Awake()
    {
        if (playerStats == null)
        {
            playerStats = GetComponent<PlayerStats>();
        }
    }

    private void OnEnable()
    {
        if (playerStats != null)
        {
            playerStats.ExpGained += ShowExp;
            playerStats.DamageTaken += ShowDamage;
        }
    }

    private void OnDisable()
    {
        if (playerStats != null)
        {
            playerStats.ExpGained -= ShowExp;
            playerStats.DamageTaken -= ShowDamage;
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

    private void ShowDamage(int amount)
    {
        if (floatingTextPrefab == null)
        {
            return;
        }

        Vector3 position = floatingTextPoint != null
            ? floatingTextPoint.position + Vector3.right * 0.25f
            : transform.position + Vector3.up * 2f + Vector3.right * 0.25f;

        FloatingTextEffect effect = Instantiate(
            floatingTextPrefab,
            position,
            Quaternion.identity
        );

        effect.Initialize($"-{amount} HP", Color.red);
    }
}