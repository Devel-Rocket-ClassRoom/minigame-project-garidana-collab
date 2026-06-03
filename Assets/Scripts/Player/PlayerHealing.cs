using System;
using UnityEngine;

public class PlayerHealing : MonoBehaviour
{
    [SerializeField]
    public PlayerStats playerStats;

    [SerializeField]
    public PlayerInputReader playerInput;

    [SerializeField]
    private int _maxHealItemCount = 5;

    [SerializeField]
    private int _healItemCount = 2;

    [SerializeField]
    private float _healAmount = 20f;

    [SerializeField]
    private float _healAmountPer10Levels = 10f;

    [SerializeField]
    private float _healCooldown = 1f;

    private float _lastUseTime = -999f;

    public event Action<int> PotionHealed;

    public int HealItemCount => _healItemCount;
    public int MaxHealItemCount => GetCurrentMaxHealItemCount();
    public float CurrentHealAmount => GetCurrentHealAmount();
    public bool IsHealOnCooldown => Time.time < _lastUseTime + _healCooldown;
    public float HealCooldownProgress
    {
        get
        {
            if (_healCooldown <= 0f)
            {
                return 1f;
            }

            float elapsed = Time.time - _lastUseTime;
            return Mathf.Clamp01(elapsed / _healCooldown);
        }
    }


    private void Awake()
    {
        if (playerStats == null)
        {
            playerStats = GetComponent<PlayerStats>();
        }

        if (playerInput == null)
        {
            playerInput = GetComponent<PlayerInputReader>();
        }
    }

    private void Update()
    {
        if (playerInput == null)
        {
            return;
        }

        if (playerStats != null && playerStats.IsDead)
        {
            playerInput.UseHealInput();
            return;
        }

        if (!playerInput.HealRequested)
        {
            return;
        }

        TryUseHeal();
        playerInput.UseHealInput();
    }

    public bool TryUseHeal()
    {
        if (playerStats == null)
        {
            return false;
        }

        if (_healItemCount <= 0)
        {
            return false;
        }

        if (Time.time < _lastUseTime + _healCooldown)
        {
            return false;
        }

        float healAmount = GetCurrentHealAmount();

        if (!playerStats.Heal(healAmount))
        {
            return false;
        }
        
        _healItemCount--;
        _lastUseTime = Time.time;
        PotionHealed?.Invoke(Mathf.RoundToInt(healAmount));
        return true;
    }

    // NPC 테스트용 물약 충전
    public void RefillHealItems()
    {
        _healItemCount = GetCurrentMaxHealItemCount();
    }

    public void SetHealItemCount(int count)
    {
        _healItemCount = Mathf.Clamp(count, 0, GetCurrentMaxHealItemCount());
        _lastUseTime = -999f;
    }

    private float GetCurrentHealAmount()
    {
        if (playerStats == null)
        {
            return _healAmount;
        }

        int levelBonusSteps = Mathf.Max(0, playerStats.Level / 10);
        return _healAmount + (levelBonusSteps * _healAmountPer10Levels);
    }

    private int GetCurrentMaxHealItemCount()
    {
        if (playerStats == null)
        {
            return _maxHealItemCount;
        }

        int levelBonusSteps = Mathf.Max(0, playerStats.Level / 10);
        return _maxHealItemCount + levelBonusSteps;
    }
}
