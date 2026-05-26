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

    [SerializeField]
    private float _effectScaleMultiplier = 1f;

    public AttackStageData()
    {
    }

    public AttackStageData(
        string stageName,
        int unlockLevel,
        float damageMultiplier,
        float hitRange,
        float hitRadius,
        Color effectColor,
        float effectScaleMultiplier = 1f
    )
    {
        _stageName = stageName;
        _unlockLevel = unlockLevel;
        _damageMultiplier = damageMultiplier;
        _hitRange = hitRange;
        _hitRadius = hitRadius;
        _effectColor = effectColor;
        _effectScaleMultiplier = effectScaleMultiplier;
    }

    public string StageName => _stageName;
    public int UnlockLevel => _unlockLevel;
    public float DamageMultiplier => Mathf.Max(0f, _damageMultiplier);
    public float HitRange => Mathf.Max(0f, _hitRange);
    public float HitRadius => Mathf.Max(0f, _hitRadius);
    public Color EffectColor => _effectColor;
    public ParticleSystem AttackEffectPrefab => _attackEffectPrefab;
    public float EffectScaleMultiplier => Mathf.Max(0.1f, _effectScaleMultiplier);
}

[System.Serializable]
public class ComboSlashEffectPose
{
    [SerializeField]
    private string _label = "Attack";

    [SerializeField]
    private Vector3 _localOffset = new Vector3(0f, 0.9f, 0.7f);

    [SerializeField]
    private Vector3 _localEulerAngles;

    [SerializeField]
    private float _scaleMultiplier = 1f;

    public ComboSlashEffectPose()
    {
    }

    public ComboSlashEffectPose(string label, Vector3 localOffset, Vector3 localEulerAngles, float scaleMultiplier = 1f)
    {
        _label = label;
        _localOffset = localOffset;
        _localEulerAngles = localEulerAngles;
        _scaleMultiplier = scaleMultiplier;
    }

    public string Label => _label;
    public Vector3 LocalOffset => _localOffset;
    public Vector3 LocalEulerAngles => _localEulerAngles;
    public float ScaleMultiplier => Mathf.Max(0.1f, _scaleMultiplier);
}

[RequireComponent(typeof(PlayerStats))]
public class PlayerAttackUpgrade : MonoBehaviour
{
    [SerializeField]
    private AttackStageData[] _attackStages =
    {
        new AttackStageData("Normal", 1, 1f, 1.1f, 0.9f, Color.white, 1f),
        new AttackStageData("Enhanced 1", 10, 1.2f, 1.25f, 1f, Color.blue, 1.1f),
        new AttackStageData("Enhanced 2", 20, 1.45f, 1.4f, 1.1f, new Color(0.55f, 0.2f, 1f), 1.2f),
        new AttackStageData("Enhanced 3", 30, 1.75f, 1.6f, 1.2f, new Color(1f, 0.78f, 0.15f), 1.35f),
        new AttackStageData("Enhanced 4", 40, 2.1f, 1.8f, 1.35f, new Color(1f, 0.25f, 0.15f), 1.5f),
    };

    [SerializeField]
    private Transform _effectSpawnPoint;

    [SerializeField]
    private ComboSlashEffectPose[] _comboEffectPoses =
    {
        new ComboSlashEffectPose("Attack 1", new Vector3(0.15f, 1.0f, 0.75f), new Vector3(18f, -10f, -55f), 1f),
        new ComboSlashEffectPose("Attack 2", new Vector3(-0.05f, 0.95f, 0.8f), new Vector3(18f, 10f, 55f), 1.05f),
        new ComboSlashEffectPose("Attack 3", new Vector3(0f, 1.0f, 0.9f), new Vector3(8f, 0f, 90f), 1.2f)
    };

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

    public void PlayAttackEffect(int comboStep, Transform attackRoot)
    {
        if (_currentStage == null || _currentStage.AttackEffectPrefab == null)
        {
            return;
        }

        Transform spawnPoint = _effectSpawnPoint != null ? _effectSpawnPoint : (attackRoot != null ? attackRoot : transform);
        ComboSlashEffectPose pose = GetComboEffectPose(comboStep);
        Quaternion rotation = spawnPoint.rotation;
        Vector3 position = spawnPoint.position;

        if (pose != null)
        {
            position = spawnPoint.TransformPoint(pose.LocalOffset);
            rotation = spawnPoint.rotation * Quaternion.Euler(pose.LocalEulerAngles);
        }

        ParticleSystem effect = Instantiate(
            _currentStage.AttackEffectPrefab,
            position,
            rotation
        );

        ParticleSystem.MainModule main = effect.main;
        main.startColor = _currentStage.EffectColor;
        effect.transform.localScale *= _currentStage.EffectScaleMultiplier * (pose != null ? pose.ScaleMultiplier : 1f);
        effect.Play();

        float lifetime = main.duration + main.startLifetime.constantMax;
        Destroy(effect.gameObject, lifetime);
    }

    private ComboSlashEffectPose GetComboEffectPose(int comboStep)
    {
        int index = comboStep - 1;
        if (_comboEffectPoses == null || index < 0 || index >= _comboEffectPoses.Length)
        {
            return null;
        }

        return _comboEffectPoses[index];
    }
}
