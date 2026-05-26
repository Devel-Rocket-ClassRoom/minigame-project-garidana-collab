using UnityEngine;

[System.Serializable]
public class AttackStageData
{
    [SerializeField]
    private string _stageName = "Normal";

    [SerializeField]
    private int _unlockLevel = 1;

    [SerializeField]
    private float _damageMultiplier = 1f;

    [SerializeField]
    private float _hitRange = 1.1f;

    [SerializeField]
    private float _hitRadius = 0.9f;

    [SerializeField]
    private Color _effectColor = Color.white;

    [SerializeField]
    private ParticleSystem _attackEffectPrefab;

    public AttackStageData()
    {
    }

    public AttackStageData(
        string stageName,
        int unlockLevel,
        float damageMultiplier,
        float hitRange,
        float hitRadius,
        Color effectColor
    )
    {
        _stageName = stageName;
        _unlockLevel = unlockLevel;
        _damageMultiplier = damageMultiplier;
        _hitRange = hitRange;
        _hitRadius = hitRadius;
        _effectColor = effectColor;
    }

    public string StageName => _stageName;
    public int UnlockLevel => _unlockLevel;
    public float DamageMultiplier => Mathf.Max(0f, _damageMultiplier);
    public float HitRange => Mathf.Max(0f, _hitRange);
    public float HitRadius => Mathf.Max(0f, _hitRadius);
    public Color EffectColor => _effectColor;
    public ParticleSystem AttackEffectPrefab => _attackEffectPrefab;
}

[RequireComponent(typeof(PlayerStats))]
public class PlayerAttackUpgrade : MonoBehaviour
{
    [SerializeField]
    private AttackStageData[] _attackStages =
    {
        new AttackStageData("Normal", 1, 1f, 1.1f, 0.9f, Color.white),
        new AttackStageData("Enhanced 1", 10, 1.2f, 1.25f, 1f, Color.blue),
        new AttackStageData("Enhanced 2", 20, 1.45f, 1.4f, 1.1f, new Color(0.55f, 0.2f, 1f)),
        new AttackStageData("Enhanced 3", 30, 1.75f, 1.6f, 1.2f, new Color(1f, 0.78f, 0.15f)),
        new AttackStageData("Enhanced 4", 40, 2.1f, 1.8f, 1.35f, new Color(1f, 0.25f, 0.15f)),
    };

    [SerializeField]
    private Transform _effectSpawnPoint;

    private PlayerStats _playerStats;
    private AttackStageData _currentStage;

    public AttackStageData CurrentStage => _currentStage;
    public string CurrentStageName => _currentStage != null ? _currentStage.StageName : "Normal";
    public float CurrentDamageMultiplier => _currentStage != null ? _currentStage.DamageMultiplier : 1f;
    public float CurrentHitRange => _currentStage != null ? _currentStage.HitRange : 1.1f;
    public float CurrentHitRadius => _currentStage != null ? _currentStage.HitRadius : 0.9f;

    private void Awake()
    {
        _playerStats = GetComponent<PlayerStats>();
        RefreshStage(_playerStats != null ? _playerStats.Level : 1);
    }

    private void OnEnable()
    {
        if (_playerStats == null)
        {
            _playerStats = GetComponent<PlayerStats>();
        }

        if (_playerStats != null)
        {
            _playerStats.LevelChanged += RefreshStage;
            RefreshStage(_playerStats.Level);
        }
    }

    private void OnDisable()
    {
        if (_playerStats != null)
        {
            _playerStats.LevelChanged -= RefreshStage;
        }
    }

    public void RefreshStage(int level)
    {
        AttackStageData selectedStage = null;

        if (_attackStages != null)
        {
            for (int i = 0; i < _attackStages.Length; i++)
            {
                AttackStageData stage = _attackStages[i];
                if (stage != null && level >= stage.UnlockLevel)
                {
                    selectedStage = stage;
                }
            }
        }

        _currentStage = selectedStage;
    }

    public void PlayAttackEffect()
    {
        if (_currentStage == null || _currentStage.AttackEffectPrefab == null)
        {
            return;
        }

        Transform spawnPoint = _effectSpawnPoint != null ? _effectSpawnPoint : transform;
        ParticleSystem effect = Instantiate(
            _currentStage.AttackEffectPrefab,
            spawnPoint.position,
            spawnPoint.rotation,
            spawnPoint
        );

        ParticleSystem.MainModule main = effect.main;
        main.startColor = _currentStage.EffectColor;
        effect.Play();

        float lifetime = main.duration + main.startLifetime.constantMax;
        Destroy(effect.gameObject, lifetime);
    }
}
