using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemTooltipUi : MonoBehaviour
{
    private static ItemTooltipUi _instance;

    private const float TooltipOffsetX = 18f;
    private const float TooltipOffsetY = 18f;
    private const float ScreenPadding = 16f;

    private Canvas _canvas;
    private RectTransform _canvasRect;
    private RectTransform _tooltipRoot;
    private CanvasGroup _tooltipCanvasGroup;
    private TextMeshProUGUI _nameText;
    private TextMeshProUGUI _descriptionText;
    private TextMeshProUGUI _tierText;
    private TextMeshProUGUI _statsText;
    private RectTransform _hintRoot;
    private TextMeshProUGUI _hintText;

    public static ItemTooltipUi Instance
    {
        get
        {
            if (_instance == null)
            {
                Debug.Log("[ItemTooltipUi] Instance: try find existing");
                _instance = FindFirstObjectByType<ItemTooltipUi>();
            }

            if (_instance == null)
            {
                Debug.Log("[ItemTooltipUi] Instance: create runtime instance");
                _instance = CreateRuntimeInstance();
            }
            else
            {
                Debug.Log("[ItemTooltipUi] Instance: reuse existing");
            }

            return _instance;
        }
    }

    public static ItemTooltipUi EnsureForCanvas(Canvas preferredCanvas)
    {
        ItemTooltipUi tooltip = Instance;
        if (tooltip == null)
        {
            return null;
        }

        if (preferredCanvas == null)
        {
            return tooltip;
        }

        RectTransform preferredRect = preferredCanvas.transform as RectTransform;
        if (tooltip._canvas == preferredCanvas && tooltip.transform.parent == preferredCanvas.transform)
        {
            return tooltip;
        }

        tooltip._canvas = preferredCanvas;
        tooltip._canvasRect = preferredRect;
        RectTransform rootRect = tooltip.transform as RectTransform;
        rootRect.SetParent(preferredCanvas.transform, false);
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        rootRect.SetAsLastSibling();
        tooltip.transform.SetAsLastSibling();
        tooltip.EnsureBuilt();
        Debug.Log($"[ItemTooltipUi] EnsureForCanvas: rebound to {preferredCanvas.name}");
        return tooltip;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        EnsureBuilt();
        transform.SetAsLastSibling();
        Hide();
    }

    public static void Show(ItemData item, Vector2 screenPosition, string actionHint = null)
    {
        if (item == null)
        {
            return;
        }

        Debug.Log($"[ItemTooltipUi] Show called: {item.displayName}");
        ItemTooltipUi tooltip = Instance;
        if (tooltip == null)
        {
            Debug.LogWarning("[ItemTooltipUi] Show aborted: tooltip instance is null");
            return;
        }

        tooltip.ShowInternal(item, screenPosition, actionHint);
    }

    public static void UpdateTooltipPosition(Vector2 screenPosition)
    {
        if (_instance == null)
        {
            return;
        }

        _instance.UpdatePosition(screenPosition);
    }

    public static void HideTooltip()
    {
        if (_instance == null)
        {
            return;
        }

        _instance.Hide();
    }

    private static ItemTooltipUi CreateRuntimeInstance()
    {
        Debug.Log("[ItemTooltipUi] CreateRuntimeInstance called");

        Canvas canvas = FindSuitableCanvas();
        if (canvas == null)
        {
            Debug.LogWarning("[ItemTooltipUi] CreateRuntimeInstance failed: no canvas found");
            return null;
        }

        Debug.Log($"[ItemTooltipUi] CreateRuntimeInstance canvas: {canvas.name}");
        GameObject root = new GameObject("ItemTooltipUi", typeof(RectTransform), typeof(ItemTooltipUi));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.SetParent(canvas.transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        ItemTooltipUi tooltip = root.GetComponent<ItemTooltipUi>();
        tooltip._canvas = canvas;
        tooltip._canvasRect = canvas.transform as RectTransform;
        tooltip.EnsureBuilt();
        tooltip.transform.SetAsLastSibling();
        tooltip.Hide();
        return tooltip;
    }

    private static Canvas FindSuitableCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] != null && canvases[i].isActiveAndEnabled && canvases[i].rootCanvas == canvases[i])
            {
                Debug.Log($"[ItemTooltipUi] FindSuitableCanvas: found {canvases[i].name}");
                return canvases[i];
            }
        }

        Debug.LogWarning("[ItemTooltipUi] FindSuitableCanvas: no suitable canvas");
        return null;
    }

    private void EnsureBuilt()
    {
        if (_canvas == null)
        {
            _canvas = GetComponentInParent<Canvas>();
        }

        if (_canvasRect == null && _canvas != null)
        {
            _canvasRect = _canvas.transform as RectTransform;
        }

        if (_tooltipRoot == null)
        {
            BuildTooltip();
        }

        if (_hintRoot == null)
        {
            BuildHint();
        }
    }

    private void BuildTooltip()
    {
        GameObject tooltipObject = new GameObject("TooltipRoot", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        _tooltipRoot = tooltipObject.GetComponent<RectTransform>();
        _tooltipRoot.SetParent(transform, false);
        _tooltipRoot.anchorMin = new Vector2(0.5f, 0.5f);
        _tooltipRoot.anchorMax = new Vector2(0.5f, 0.5f);
        _tooltipRoot.pivot = new Vector2(0f, 0f);
        _tooltipRoot.sizeDelta = new Vector2(320f, 0f);

        Image background = tooltipObject.GetComponent<Image>();
        background.color = new Color(0.07f, 0.05f, 0.02f, 0.96f);
        background.raycastTarget = false;

        _tooltipCanvasGroup = tooltipObject.GetComponent<CanvasGroup>();
        _tooltipCanvasGroup.interactable = false;
        _tooltipCanvasGroup.blocksRaycasts = false;

        VerticalLayoutGroup layout = tooltipObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 16, 16);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = tooltipObject.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _nameText = CreateText("NameText", 28, FontStyles.Bold, new Color(1f, 0.92f, 0.68f));
        _descriptionText = CreateText("DescriptionText", 22, FontStyles.Normal, Color.white);
        _tierText = CreateText("TierText", 22, FontStyles.Normal, new Color(0.91f, 0.84f, 0.62f));
        _statsText = CreateText("StatsText", 22, FontStyles.Normal, new Color(0.8f, 1f, 0.82f));
    }

    private void BuildHint()
    {
        GameObject hintObject = new GameObject("ActionHint", typeof(RectTransform), typeof(Image));
        _hintRoot = hintObject.GetComponent<RectTransform>();
        _hintRoot.SetParent(transform, false);
        _hintRoot.anchorMin = new Vector2(1f, 0f);
        _hintRoot.anchorMax = new Vector2(1f, 0f);
        _hintRoot.pivot = new Vector2(1f, 0f);
        _hintRoot.anchoredPosition = new Vector2(-42f, 42f);
        _hintRoot.sizeDelta = new Vector2(280f, 56f);

        Image background = hintObject.GetComponent<Image>();
        background.color = new Color(0.12f, 0.08f, 0.03f, 0.92f);
        background.raycastTarget = false;

        GameObject textObject = new GameObject("HintText", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(_hintRoot, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(16f, 10f);
        textRect.offsetMax = new Vector2(-16f, -10f);

        _hintText = textObject.GetComponent<TextMeshProUGUI>();
        _hintText.font = TMP_Settings.defaultFontAsset;
        _hintText.fontSize = 22f;
        _hintText.alignment = TextAlignmentOptions.MidlineRight;
        _hintText.color = new Color(1f, 0.95f, 0.8f);
        _hintText.raycastTarget = false;
    }

    private TextMeshProUGUI CreateText(string objectName, float fontSize, FontStyles fontStyle, Color color)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(_tooltipRoot, false);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = color;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }

    private void ShowInternal(ItemData item, Vector2 screenPosition, string actionHint)
    {
        Debug.Log($"[ItemTooltipUi] ShowInternal: {item.displayName}");
        EnsureBuilt();
        if (_tooltipRoot == null || _tooltipCanvasGroup == null)
        {
            Debug.LogWarning("[ItemTooltipUi] ShowInternal aborted: tooltip root or canvas group missing");
            return;
        }

        _nameText.text = item.displayName;
        _descriptionText.text = string.IsNullOrWhiteSpace(item.description) ? "-" : item.description;
        _tierText.text = $"Tier {item.tier}";
        _statsText.text = $"ATK +{item.attackBonus:F0}\nHP +{item.maxHealthBonus:F0}";

        _tooltipRoot.gameObject.SetActive(true);
        _tooltipCanvasGroup.alpha = 1f;
        transform.SetAsLastSibling();
        _tooltipRoot.SetAsLastSibling();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_tooltipRoot);
        UpdatePosition(screenPosition);
        Debug.Log($"[ItemTooltipUi] Tooltip shown at {_tooltipRoot.anchoredPosition}");

        bool hasHint = !string.IsNullOrWhiteSpace(actionHint);
        if (_hintRoot != null)
        {
            _hintRoot.gameObject.SetActive(hasHint);
        }

        if (_hintText != null)
        {
            _hintText.text = actionHint;
        }
    }

    private void UpdatePosition(Vector2 screenPosition)
    {
        if (_canvasRect == null || _tooltipRoot == null)
        {
            return;
        }

        Camera uiCamera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay ? _canvas.worldCamera : null;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPosition, uiCamera, out Vector2 localPoint);

        float width = _tooltipRoot.rect.width;
        float height = _tooltipRoot.rect.height;
        Rect canvasBounds = _canvasRect.rect;

        float leftBound = canvasBounds.xMin + ScreenPadding;
        float rightBound = canvasBounds.xMax - ScreenPadding;
        float bottomBound = canvasBounds.yMin + ScreenPadding;
        float topBound = canvasBounds.yMax - ScreenPadding;

        float desiredX = localPoint.x + TooltipOffsetX;
        float desiredY = localPoint.y + TooltipOffsetY;

        if (desiredX + width > rightBound)
        {
            desiredX = localPoint.x - TooltipOffsetX - width;
        }

        if (desiredY + height > topBound)
        {
            desiredY = localPoint.y - TooltipOffsetY - height;
        }

        float minX = leftBound;
        float maxX = rightBound - width;
        float minY = bottomBound;
        float maxY = topBound - height;

        desiredX = Mathf.Clamp(desiredX, minX, Mathf.Max(minX, maxX));
        desiredY = Mathf.Clamp(desiredY, minY, Mathf.Max(minY, maxY));
        _tooltipRoot.anchoredPosition = new Vector2(desiredX, desiredY);
    }

    private void Hide()
    {
        Debug.Log("[ItemTooltipUi] Hide");
        if (_tooltipRoot != null)
        {
            _tooltipRoot.gameObject.SetActive(false);
        }

        if (_hintRoot != null)
        {
            _hintRoot.gameObject.SetActive(false);
        }
    }
}
