using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class InventorySlotUi : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler, IPointerClickHandler
{
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private Button _button;
    // 장착 중일 때 보여줄 하이라이트 오브젝트 (테두리 이미지 등)
    [SerializeField] private GameObject _equippedHighlight;

    private ItemData _data;
    private Action<ItemData> _onClicked;
    private bool _canClick;

    private void Awake()
    {
        if (_iconImage != null)
        {
            _iconImage.raycastTarget = false;
        }

        if (_equippedHighlight != null)
        {
            Graphic highlightGraphic = _equippedHighlight.GetComponent<Graphic>();
            if (highlightGraphic != null)
            {
                highlightGraphic.raycastTarget = false;
            }
        }
    }

    public void Setup(ItemData data, bool isEquipped, Action<ItemData> onClicked, Sprite backgroundSprite = null)
    {
        _data      = data;
        _onClicked = onClicked;
        _canClick = data != null && data.IsEquipment;

        if (_backgroundImage != null && backgroundSprite != null)
        {
            _backgroundImage.sprite = backgroundSprite;
        }

        if (_iconImage != null)
        {
            _iconImage.sprite  = data != null ? data.icon : null;
            _iconImage.enabled = data != null && data.icon != null;
        }

        if (_nameText != null)
            _nameText.text = data != null ? data.displayName : "-";

        if (_equippedHighlight != null)
            _equippedHighlight.SetActive(_canClick && isEquipped);

        if (_button != null)
            _button.interactable = _canClick;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_data == null)
        {
            return;
        }

        Debug.Log($"[InventorySlotUi] Hover: {_data.displayName}");
        ItemTooltipUi.Show(_data, eventData.position, _canClick ? "[Right Click to Equip]" : null);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ItemTooltipUi.HideTooltip();
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (_data == null)
        {
            return;
        }

        ItemTooltipUi.UpdateTooltipPosition(eventData.position);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_canClick || _data == null)
        {
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            _onClicked?.Invoke(_data);
        }
    }
}

/*



*/