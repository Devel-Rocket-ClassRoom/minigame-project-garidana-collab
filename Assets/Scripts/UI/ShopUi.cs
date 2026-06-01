using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ShopUi : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject _panelRoot;
    [SerializeField] private Transform _contentRoot;
    [SerializeField] private ShopItemSlotUi _slotTemplate;
    [SerializeField] private TextMeshProUGUI _dialogueText;

    private readonly List<ShopItemSlotUi> _slots = new List<ShopItemSlotUi>();
    private string _defaultDialogueText;
    private IReadOnlyList<ItemData> _currentStock;
    private PlayerInventory _buyerInventory;
    private PlayerStats _buyerStats;
    private Transform _buyerTransform;
    private MerchantInteractable _currentMerchant;
    private float _closeDistance;
    private bool _isOpen;
    private static int _lastClosedFrame = -1;

    public bool IsOpen => _isOpen;
    public MerchantInteractable CurrentMerchant => _currentMerchant;
    public static bool BlocksGlobalShortcuts => IsAnyOpen() || _lastClosedFrame == Time.frameCount;

    private void Awake()
    {
        ResolveReferences();
        if (!_isOpen)
        {
            SetOpen(false);
        }
    }

    private void Update()
    {
        if (!_isOpen)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            Close();
            return;
        }

        if (IsBuyerOutOfRange())
        {
            Close();
        }
    }

    public static ShopUi Ensure()
    {
        ShopUi existing = FindExistingShopUi();
        if (existing != null)
        {
            existing.ResolveReferences();
            return existing;
        }

        GameObject window = FindGameObjectByNameIncludingInactive("ShopWindow");
        if (window == null)
        {
            window = new GameObject("ShopWindow", typeof(RectTransform), typeof(Image));
            Canvas canvas = FindExistingCanvas();
            if (canvas != null)
            {
                RectTransform rect = window.GetComponent<RectTransform>();
                rect.SetParent(canvas.transform, false);
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(720f, 560f);
            }
        }

        ShopUi shopUi = window.AddComponent<ShopUi>();
        shopUi.ResolveReferences();
        return shopUi;
    }

    public void Open(MerchantInteractable merchant, GameObject buyer)
    {
        if (merchant == null || buyer == null)
        {
            return;
        }

        _currentMerchant = merchant;
        _buyerInventory = buyer.GetComponentInParent<PlayerInventory>();
        _buyerStats = buyer.GetComponentInParent<PlayerStats>();
        _buyerTransform = buyer.transform;
        PlayerInteractor playerInteractor = buyer.GetComponentInParent<PlayerInteractor>();
        _closeDistance = playerInteractor != null ? playerInteractor.InteractRadius : 2f;
        _currentStock = merchant.Stock;

        ResolveReferences();
        ResetMerchantDialogue();
        Refresh();
        SetOpen(true);
    }

    public void Close()
    {
        ResetMerchantDialogue();
        SetOpen(false);
        _lastClosedFrame = Time.frameCount;
        _currentMerchant = null;
        _currentStock = null;
        _buyerInventory = null;
        _buyerStats = null;
        _buyerTransform = null;
        _closeDistance = 0f;
    }

    private void Refresh()
    {
        if (_contentRoot == null)
        {
            Debug.LogWarning("[ShopUi] ContentRoot가 없어 상점 목록을 그릴 수 없습니다.");
            return;
        }

        IReadOnlyList<ItemData> stock = _currentStock ?? System.Array.Empty<ItemData>();
        EnsureSlotCount(stock.Count);

        for (int i = 0; i < _slots.Count; i++)
        {
            bool hasItem = i < stock.Count && stock[i] != null;
            _slots[i].gameObject.SetActive(hasItem);

            if (hasItem)
            {
                _slots[i].Setup(stock[i], TryBuy);
            }
        }
    }

    private void TryBuy(ItemData item)
    {
        if (item == null || _buyerInventory == null || _buyerStats == null)
        {
            return;
        }

        if (_buyerInventory.IsFull)
        {
            Debug.Log("[Shop] 인벤토리가 가득 차서 구매할 수 없습니다.");
            return;
        }

        if (!_buyerStats.SpendGold(item.price))
        {
            Debug.Log($"[Shop] 골드가 부족합니다. 필요 골드: {item.price}, 현재 골드: {_buyerStats.Gold}");
            SetMerchantDialogue("골드가 부족하군..");
            return;
        }

        if (_buyerInventory.AddItem(item))
        {
            if (_currentMerchant != null)
            {
                _currentMerchant.RemoveStockItem(item);
            }

            Debug.Log($"[Shop] 구매 완료: {item.displayName} ({item.price} G)");
            SetMerchantDialogue($"{item.displayName}을 구매했군.");
            Refresh();
            return;
        }

        Debug.LogWarning($"[Shop] 골드를 지불했지만 인벤토리 추가에 실패했습니다: {item.displayName}");
    }

    private void EnsureSlotCount(int count)
    {
        if (_slotTemplate == null)
        {
            _slotTemplate = CreateSlotTemplate();
        }

        while (_slots.Count < count)
        {
            ShopItemSlotUi slot = Instantiate(_slotTemplate, _contentRoot);
            slot.gameObject.SetActive(true);
            _slots.Add(slot);
        }
    }

    private ShopItemSlotUi CreateSlotTemplate()
    {
        Transform templateTransform = _contentRoot != null ? _contentRoot.Find("MerchantItemSlotUi") : null;
        GameObject templateObject = templateTransform != null
            ? templateTransform.gameObject
            : new GameObject("MerchantItemSlotUi", typeof(RectTransform), typeof(Image));

        if (_contentRoot != null && templateObject.transform.parent != _contentRoot)
        {
            templateObject.transform.SetParent(_contentRoot, false);
        }

        ShopItemSlotUi slot = templateObject.GetComponent<ShopItemSlotUi>();
        if (slot == null)
        {
            slot = templateObject.AddComponent<ShopItemSlotUi>();
        }

        templateObject.SetActive(false);
        return slot;
    }

    private void ResolveReferences()
    {
        if (_panelRoot == null)
        {
            _panelRoot = gameObject;
        }

        if (_contentRoot == null)
        {
            Transform content = FindChildRecursive(transform, "Content");
            _contentRoot = content != null ? content : transform;
        }

        if (_contentRoot != null)
        {
            VerticalLayoutGroup layout = _contentRoot.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = _contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(12, 12, 12, 12);
                layout.spacing = 12f;
                layout.childControlWidth = true;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
            }

            ContentSizeFitter fitter = _contentRoot.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = _contentRoot.gameObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }

        if (_slotTemplate == null && _contentRoot != null)
        {
            _slotTemplate = _contentRoot.GetComponentInChildren<ShopItemSlotUi>(true);
        }

        if (_dialogueText == null)
        {
            Transform dialogueRoot = FindChildRecursive(transform, "NPCDialogue");
            if (dialogueRoot != null)
            {
                _dialogueText = dialogueRoot.GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }

        if (_dialogueText != null && _defaultDialogueText == null)
        {
            _defaultDialogueText = _dialogueText.text;
        }

        _ = ItemTooltipUi.EnsureForCanvas(GetComponentInParent<Canvas>());
    }

    private void SetOpen(bool open)
    {
        _isOpen = open;

        if (_panelRoot != null)
        {
            _panelRoot.SetActive(open);
        }

        if (!open)
        {
            ItemTooltipUi.HideTooltip();
        }
    }

    private bool IsBuyerOutOfRange()
    {
        if (_currentMerchant == null || _buyerTransform == null || _closeDistance <= 0f)
        {
            return false;
        }

        return Vector3.Distance(_buyerTransform.position, _currentMerchant.Transform.position) > _closeDistance;
    }

    private static Transform FindChildRecursive(Transform root, string targetName)
    {
        if (root == null)
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == targetName)
            {
                return child;
            }

            Transform found = FindChildRecursive(child, targetName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private void SetMerchantDialogue(string message)
    {
        if (_dialogueText == null || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        _dialogueText.text = message;
    }

    private void ResetMerchantDialogue()
    {
        if (_dialogueText == null)
        {
            return;
        }

        _dialogueText.text = _defaultDialogueText ?? string.Empty;
    }

    private static ShopUi FindExistingShopUi()
    {
        ShopUi[] shopUis = FindObjectsByType<ShopUi>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        return shopUis.Length > 0 ? shopUis[0] : null;
    }

    public static bool IsAnyOpen()
    {
        ShopUi[] shopUis = FindObjectsByType<ShopUi>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < shopUis.Length; i++)
        {
            if (shopUis[i] != null && shopUis[i]._isOpen)
            {
                return true;
            }
        }

        return false;
    }

    private static GameObject FindGameObjectByNameIncludingInactive(string objectName)
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != null && transforms[i].name == objectName)
            {
                return transforms[i].gameObject;
            }
        }

        return null;
    }

    private static Canvas FindExistingCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        return canvases.Length > 0 ? canvases[0] : null;
    }
}
