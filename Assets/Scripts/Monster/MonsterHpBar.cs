using UnityEngine;
using UnityEngine.UI;   

public class MonsterHpBar : MonoBehaviour
{
    [SerializeField]
    private Slider hpSlider;

    private Transform _cam;
    private BaseMonster _monster;

    private void Start()
    {
        _cam = Camera.main.transform;

        _monster = GetComponentInParent<BaseMonster>();

        if (_monster != null)
        {
            _monster.OnHpChanged += UpdateBar;

            UpdateBar(1f);
        }

    }

    private void LateUpdate()
    {
        transform.LookAt(transform.position + _cam.forward);  
    }

    private void UpdateBar(float ratio)
    {
        hpSlider.value = ratio;
    }

    private void OnDestroy()
    {
        if (_monster != null)
        {
            _monster.OnHpChanged -= UpdateBar;
        }
    }
}
