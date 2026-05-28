using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHud : MonoBehaviour
{

    [SerializeField] 
    private PlayerStats _playerStats;

    [SerializeField] 
    private FloatingHudGoldTextEffect goldFloatingTextPrefab;

    [SerializeField]
    private RectTransform goldFloatingTextParent;

    private int goldFloatingTextStack;

    public Slider healthSlider;
    public Slider expSlider;
    public Slider staminaSlider;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI staminaText;
    public TextMeshProUGUI expText;

    private void OnEnable()
    {
        if (_playerStats != null)
        {
            _playerStats.GoldGained += ShowGoldGain;
            _playerStats.GoldSpent += ShowGoldSpent;
        }
    }

    private void OnDisable()
    {
        if (_playerStats != null)
        {
            _playerStats.GoldGained -= ShowGoldGain;
            _playerStats.GoldSpent -= ShowGoldSpent;
        }
    }

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

    private void ShowGoldGain(int amount)
    {
        ShowGoldFloatingText($"+{amount} G", Color.yellow);
    }

    private void ShowGoldSpent(int amount)
    {
        ShowGoldFloatingText($"-{amount} G", Color.red);
    }

    private void ShowGoldFloatingText(string value, Color color)
    {
        if (goldFloatingTextPrefab == null || goldText == null)
        {
            return;
        }

        Transform parent = goldFloatingTextParent != null
            ? goldFloatingTextParent
            : goldText.transform.parent;

        FloatingHudGoldTextEffect effect = Instantiate(goldFloatingTextPrefab, parent);

        RectTransform effectRect = effect.GetComponent<RectTransform>();
        RectTransform goldRect = goldText.GetComponent<RectTransform>();

        effectRect.position = goldRect.position;

        effectRect.anchoredPosition += Vector2.up * (goldFloatingTextStack * 24f);

        goldFloatingTextStack++;

        effect.Initialize(value, color);

        StartCoroutine(ReleaseGoldFloatingTextSlot()); 
    }

    private IEnumerator ReleaseGoldFloatingTextSlot()
    {
        yield return new WaitForSeconds(0.8f);
        goldFloatingTextStack = Mathf.Max(0, goldFloatingTextStack - 1);
    }
}
