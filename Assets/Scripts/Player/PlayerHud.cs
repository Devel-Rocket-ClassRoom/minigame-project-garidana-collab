using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHud : MonoBehaviour
{

    [SerializeField] 
    private PlayerStats _playerStats;

    public Slider healthSlider;
    public Slider expSlider;
    public TextMeshProUGUI levelText;

    private void Awake()
    {
        healthSlider.minValue = 0;
        expSlider.minValue = 0;
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

        levelText.text = $"Lv. {_playerStats.Level}";
    }
}
