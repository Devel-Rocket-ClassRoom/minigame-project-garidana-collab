using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHud : MonoBehaviour
{

    [SerializeField] 
    private PlayerStats _playerStats;

    public Slider healthSlider;
    public Slider expSlider;
    public Slider staminaSlider;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI staminaText;
    public TextMeshProUGUI expText;

    private void Awake()
    {
        healthSlider.minValue = 0;
        expSlider.minValue = 0;
        staminaSlider.minValue = 0;
        staminaSlider.maxValue = _playerStats.MaxStamina;
    }

    void Start()
    {
        
    }

    void Update()
    {
        // 체력 바
        healthSlider.maxValue = _playerStats.MaxHealth;
        healthSlider.value = _playerStats.CurrentHealth;
        // 경험치 바
        expSlider.maxValue = _playerStats.ExpToLevelUp;
        expSlider.value = _playerStats.CurrentExp;
        // 스태미나 바
        staminaSlider.value = _playerStats.CurrentStamina;
        // 텍스트 내용
        hpText.text = $"{_playerStats.CurrentHealth} / {_playerStats.MaxHealth}";
        staminaText.text = $"{_playerStats.CurrentStamina:F0} / {_playerStats.MaxStamina}";
        expText.text = $"{_playerStats.CurrentExp} / {_playerStats.ExpToLevelUp}";

        goldText.text = $"{_playerStats.Gold} G";

        levelText.text = $"Lv. {_playerStats.Level}";
    }
}
