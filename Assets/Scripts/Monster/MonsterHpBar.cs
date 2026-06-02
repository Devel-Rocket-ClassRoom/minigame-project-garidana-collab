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
    private BossMonster _bossMonster;

    private void Start()
    {
        if (Camera.main != null)
        {
            _cam = Camera.main.transform;
        }

        _monster = GetComponentInParent<BaseMonster>();
        _bossMonster = GetComponentInParent<BossMonster>();

        if (_monster != null)
        {
            _monster.OnHpChanged += UpdateBar;

            UpdateBar(1f);
            UpdateName();
        }
        else if (_bossMonster != null)
        {
            _bossMonster.OnHpChanged += UpdateBar;

            UpdateBar(1f);
            UpdateName();
        }

    }

    private void Update()
    {
        if ((_monster != null && _monster.IsDead) || (_bossMonster != null && _bossMonster.IsDead))
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

        MonsterData data = null;

        if (_monster != null)
        {
            data = _monster.data;
        }
        else if (_bossMonster != null)
        {
            data = _bossMonster.data;
        }

        if (data == null)
        {
            nameText.text = string.Empty;
            return;
        }

        nameText.text = string.IsNullOrWhiteSpace(data.monsterName)
            ? data.name
            : data.monsterName;
    }

    private void OnDestroy()
    {
        if (_monster != null)
        {
            _monster.OnHpChanged -= UpdateBar;
        }

        if (_bossMonster != null)
        {
            _bossMonster.OnHpChanged -= UpdateBar;
        }
    }


}
