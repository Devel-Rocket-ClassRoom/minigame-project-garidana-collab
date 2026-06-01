using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopItemSlotUi : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler, IPointerClickHandler
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _priceText;

    private ItemData _item;
    private Action<ItemData> _onBuyRequested;

    private void Awake()
    {
        EnsureVisuals();
    }

    public void Setup(ItemData item, Action<ItemData> onBuyRequested)
    {
        EnsureVisuals();
        DisableChildRaycastTargets();

        _item = item;
        _onBuyRequested = onBuyRequested;

        if (_iconImage != null)
        {
            _iconImage.sprite = item != null ? item.icon : null;
            _iconImage.enabled = item != null && item.icon != null;
            _iconImage.raycastTarget = false;
        }

        if (_nameText != null)
        {
            _nameText.raycastTarget = false;
            _nameText.text = item != null ? item.displayName : "-";
        }

        if (_priceText != null)
        {
            _priceText.raycastTarget = false;
            _priceText.text = item != null ? $"{item.price} G" : "-";
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_item == null)
        {
            return;
        }

        ItemTooltipUi.Show(_item, eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ItemTooltipUi.HideTooltip();
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (_item == null)
        {
            return;
        }

        ItemTooltipUi.UpdateTooltipPosition(eventData.position);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_item == null || eventData.button != PointerEventData.InputButton.Right)
        {
            return;
        }

        _onBuyRequested?.Invoke(_item);
    }

    private void EnsureVisuals()
    {
        if (_iconImage != null && _nameText != null && _priceText != null)
        {
            return;
        }

        RectTransform rect = transform as RectTransform;
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, Mathf.Max(rect.sizeDelta.y, 92f));
        }

        HorizontalLayoutGroup layout = GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
        {
            layout = gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 12, 12);
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        if (_iconImage == null)
        {
            GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            iconObject.transform.SetParent(transform, false);

            LayoutElement iconLayout = iconObject.GetComponent<LayoutElement>();
            iconLayout.preferredWidth = 64f;
            iconLayout.preferredHeight = 64f;

            _iconImage = iconObject.GetComponent<Image>();
            _iconImage.preserveAspect = true;
            _iconImage.raycastTarget = false;
        }

        if (_nameText == null)
        {
            _nameText = CreateText("NameText", 26f, TextAlignmentOptions.MidlineLeft);
            LayoutElement nameLayout = _nameText.gameObject.AddComponent<LayoutElement>();
            nameLayout.flexibleWidth = 1f;
        }

        if (_priceText == null)
        {
            _priceText = CreateText("PriceText", 24f, TextAlignmentOptions.MidlineRight);
            LayoutElement priceLayout = _priceText.gameObject.AddComponent<LayoutElement>();
            priceLayout.preferredWidth = 140f;
        }
    }

    private TextMeshProUGUI CreateText(string objectName, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(transform, false);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    private void DisableChildRaycastTargets()
    {
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] == null || graphics[i].transform == transform)
            {
                continue;
            }

            graphics[i].raycastTarget = false;
        }
    }
}
