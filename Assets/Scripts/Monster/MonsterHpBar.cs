using TMPro;
using UnityEngine;
using UnityEngine.UI;   

public class MonsterHpBar : MonoBehaviour
{
    [SerializeField]
    private Slider hpSlider;

    [SerializeField]
    private TextMeshProUGUI nameText;

    private Transform _cam;
    private BaseMonster _monster;

    private void Start()
    {
        if (Camera.main != null)
        {
            _cam = Camera.main.transform;
        }

        _monster = GetComponentInParent<BaseMonster>();

        if (_monster != null)
        {
            _monster.OnHpChanged += UpdateBar;

            UpdateBar(1f);
            UpdateName();
        }

    }

    private void Update()
    {
        if (_monster != null && _monster.IsDead)
        {
            gameObject.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        if (_cam == null)
        {
            return;
        }

        transform.LookAt(transform.position + _cam.forward);  
    }

    private void UpdateBar(float ratio)
    {
        hpSlider.value = ratio;
    }

    private void UpdateName()
    {
        if (nameText == null)
        {
            return;
        }

        if (_monster == null || _monster.data == null)
        {
            nameText.text = string.Empty;
            return;
        }

        nameText.text = string.IsNullOrWhiteSpace(_monster.data.monsterName)
            ? _monster.data.name
            : _monster.data.monsterName;
    }

    private void OnDestroy()
    {
        if (_monster != null)
        {
            _monster.OnHpChanged -= UpdateBar;
        }
    }


}
