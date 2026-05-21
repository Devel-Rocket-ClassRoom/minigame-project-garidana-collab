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
        healthSlider.maxValue = _playerStats.MaxHealth;
        healthSlider.value = _playerStats.CurrentHealth;

        expSlider.maxValue = _playerStats.ExpToLevelUp;
        expSlider.value = _playerStats.CurrentExp;

        staminaSlider.value = _playerStats.CurrentStamina;

        goldText.text = $"{_playerStats.Gold} G";

        levelText.text = $"Lv. {_playerStats.Level}";
    }
}
