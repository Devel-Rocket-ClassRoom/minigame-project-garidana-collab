using UnityEngine;
using UnityEngine.UI;

public class BossHpBarUi : MonoBehaviour
{
    [SerializeField] private Slider hpSlider;
    [SerializeField] private GameObject root;

    private BossMonster boss;
    private PlayerStats playerStats;

    private void Awake()
    {
        playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats != null)
        {
            playerStats.Died += HandlePlayerDied;
        }

        Hide();
    }

    public void Show(BossMonster targetBoss)
    {
        if (targetBoss == null)
        {
            return;
        }

        if (boss != null)
        {
            boss.OnHpChanged -= UpdateHp;
        }

        boss = targetBoss;
        boss.OnHpChanged += UpdateHp;

        SetVisible(true);
        UpdateHp(1f);
    }

    public void Hide()
    {
        if (boss != null)
        {
            boss.OnHpChanged -= UpdateHp;
            boss = null;
        }

        SetVisible(false);
    }

    private void UpdateHp(float ratio)
    {
        if (hpSlider != null)
        {
            hpSlider.value = ratio;
        }

        if (boss != null && boss.IsDead)
        {
            Hide();
        }
    }

    private void SetVisible(bool visible)
    {
        if (root != null)
        {
            root.SetActive(visible);
        }
        else
        {
            gameObject.SetActive(visible);
        }

        if (hpSlider != null)
        {
            hpSlider.gameObject.SetActive(visible);
        }
    }

    private void OnDestroy()
    {
        if (boss != null)
        {
            boss.OnHpChanged -= UpdateHp;
        }

        if (playerStats != null)
        {
            playerStats.Died -= HandlePlayerDied;
        }
    }

    private void HandlePlayerDied()
    {
        Hide();
    }
}
