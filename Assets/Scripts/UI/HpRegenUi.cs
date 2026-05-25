using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HpRegenUi : MonoBehaviour
{
    [SerializeField]
    public PlayerHealing playerHealing;

    // [SerializeField]
    // private Image _cooldownImage;

    [SerializeField]
    private TextMeshProUGUI _countText;

    private void Awake()
    {
        // if (_cooldownImage == null)
        // {
        //     _cooldownImage = GetComponent<Image>();
        // }

        // if (_cooldownImage != null)
        // {
        //     _cooldownImage.type = Image.Type.Filled;
        //     _cooldownImage.fillMethod = Image.FillMethod.Radial360;
        // }
    }

    private void Update()
    {
        if (playerHealing == null)
        {
            return;
        }

        // if (_cooldownImage != null)
        // {
        //     _cooldownImage.fillAmount = playerHealing.HealCooldownProgress;
        // }

        if (_countText != null)
        {
            _countText.text = playerHealing.HealItemCount.ToString();
        }

        float progress = playerHealing.HealCooldownProgress;
        gameObject.GetComponent<Image>().fillAmount = progress;
    }
}
