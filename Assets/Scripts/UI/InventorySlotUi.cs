using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlotUi : MonoBehaviour
{
    [SerializeField] private Image           _iconImage;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private Button          _button;
    // 장착 중일 때 보여줄 하이라이트 오브젝트 (테두리 이미지 등)
    [SerializeField] private GameObject      _equippedHighlight;

    private ItemData          _data;
    private Action<ItemData>  _onClicked;

    public void Setup(ItemData data, bool isEquipped, Action<ItemData> onClicked)
    {
        _data      = data;
        _onClicked = onClicked;

        if (_iconImage != null)
        {
            _iconImage.sprite  = data.icon;
            _iconImage.enabled = data.icon != null;
        }

        if (_nameText != null)
            _nameText.text = data.displayName;

        if (_equippedHighlight != null)
            _equippedHighlight.SetActive(isEquipped);

        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(HandleClick);
    }

    private void HandleClick()
    {
        _onClicked?.Invoke(_data);
    }
}
