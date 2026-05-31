using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUi : MonoBehaviour
{
    private const string EquippedIconChildName = "EquippedIcon";
    private static readonly EquipmentPart[] InventoryRowOrder =
    {
        EquipmentPart.Sword,
        EquipmentPart.Shield,
        EquipmentPart.Helmet,
        EquipmentPart.Chest,
        EquipmentPart.Legs
    };

    [Header("References")]
    [SerializeField] private PlayerInventory  _inventory;
    [SerializeField] private EquipmentManager _equipment;
    [SerializeField] private PlayerInputReader _playerInputReader;
    [SerializeField] private PlayerStats _playerStats;
    [SerializeField] private PlayerAttackUpgrade _playerAttackUpgrade;

    [Header("UI")]
    // 인벤토리 전체 패널 루트 (SetActive 토글 대상)
    [SerializeField] private GameObject      _panelRoot;
    // 슬롯 하나짜리 프리팹 (Button + Image + TextMeshProUGUI)
    [SerializeField] private InventorySlotUi _slotPrefab;

    [Header("Inventory Rows")]
    [SerializeField] private Transform _swordSlotRow;
    [SerializeField] private Transform _shieldSlotRow;
    [SerializeField] private Transform _helmetSlotRow;
    [SerializeField] private Transform _chestSlotRow;
    [SerializeField] private Transform _legsSlotRow;

    [Header("Inventory Slot Backgrounds")]
    [SerializeField] private Sprite _swordSlotBackground;
    [SerializeField] private Sprite _shieldSlotBackground;
    [SerializeField] private Sprite _helmetSlotBackground;
    [SerializeField] private Sprite _chestSlotBackground;
    [SerializeField] private Sprite _legsSlotBackground;
    [SerializeField] private int _slotsPerRow = 6;

    [Header("현재 장착 슬롯")]
    [SerializeField] private Image _equippedSwordIcon;
    [SerializeField] private Image _equippedShieldIcon;
    [SerializeField] private Image _equippedHelmetIcon;
    [SerializeField] private Image _equippedChestIcon;
    [SerializeField] private Image _equippedLegsIcon;

    [Header("Stat UI")]
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private TextMeshProUGUI _attackText;
    [SerializeField] private TextMeshProUGUI _hpText;
    [SerializeField] private TextMeshProUGUI _upgradeStageText;
    [SerializeField] private TextMeshProUGUI[] _completedQuestTexts;

    private readonly Dictionary<EquipmentPart, List<InventorySlotUi>> _slotsByPart = new Dictionary<EquipmentPart, List<InventorySlotUi>>();
    private readonly Dictionary<EquipmentPart, List<ItemData>> _ownedEquipmentByPart = new Dictionary<EquipmentPart, List<ItemData>>();
    private readonly Dictionary<EquipmentPart, Image> _equippedSlotImages = new Dictionary<EquipmentPart, Image>();
    private readonly Dictionary<EquipmentPart, Image> _equippedIconImages = new Dictionary<EquipmentPart, Image>();
    private readonly HashSet<Transform> _preparedRows = new HashSet<Transform>();
    private bool _isOpen = false;

    private void Awake()
    {
        if (_inventory == null)
            _inventory = FindFirstObjectByType<PlayerInventory>();
        if (_equipment == null)
            _equipment = FindFirstObjectByType<EquipmentManager>();
        if (_playerInputReader == null)
            _playerInputReader = FindFirstObjectByType<PlayerInputReader>();
        if (_playerStats == null)
            _playerStats = FindFirstObjectByType<PlayerStats>();
        if (_playerAttackUpgrade == null)
            _playerAttackUpgrade = FindFirstObjectByType<PlayerAttackUpgrade>();

        _ = ItemTooltipUi.EnsureForCanvas(GetComponentInParent<Canvas>());
        CacheEquippedSlotImages();
        SetPanelActive(false);
    }

    private void OnEnable()
    {
        if (_inventory != null) _inventory.InventoryChanged  += RefreshSlots;
        if (_equipment != null) _equipment.EquipmentChanged  += RefreshSlots;
        if (_playerInputReader != null) _playerInputReader.InventoryPressed += TogglePanel;
    }

    private void OnDisable()
    {
        if (_inventory != null) _inventory.InventoryChanged  -= RefreshSlots;
        if (_equipment != null) _equipment.EquipmentChanged  -= RefreshSlots;
        if (_playerInputReader != null) _playerInputReader.InventoryPressed -= TogglePanel;
        ItemTooltipUi.HideTooltip();
    }

    public void TogglePanel()
    {
        SetPanelActive(!_isOpen);
    }

    private void SetPanelActive(bool active)
    {
        _isOpen = active;
        if (_panelRoot != null)
            _panelRoot.SetActive(active);

        if (!active)
        {
            ItemTooltipUi.HideTooltip();
        }

        if (active)
        {
            RefreshSlots();
            RefreshStatSection();
        }
    }

    private void LateUpdate()
    {
        if (_isOpen)
        {
            RefreshStatSection();
        }
    }

    private void RefreshSlots()
    {
        RefreshEquippedIcons();

        if (_inventory == null || _slotPrefab == null) return;

        CollectOwnedEquipmentItems();
        for (int i = 0; i < InventoryRowOrder.Length; i++)
        {
            EquipmentPart part = InventoryRowOrder[i];
            RefreshPartSlots(part);
        }
    }

    private void RefreshStatSection()
    {
        RefreshPlayerStatTexts();
        RefreshCompletedQuestTexts();
    }

    private void RefreshPlayerStatTexts()
    {
        if (_playerStats != null)
        {
            if (_levelText != null)
                _levelText.text = _playerStats.Level.ToString();

            if (_attackText != null)
                _attackText.text = _playerStats.AttackPower.ToString("F0");

            if (_hpText != null)
                _hpText.text = $"{_playerStats.CurrentHealth:F0} / {_playerStats.MaxHealth:F0}";
        }

        if (_upgradeStageText != null)
        {
            _upgradeStageText.text = _playerAttackUpgrade != null
                ? _playerAttackUpgrade.CurrentStageName
                : "-";
        }
    }

    private void CollectOwnedEquipmentItems()
    {
        for (int i = 0; i < InventoryRowOrder.Length; i++)
        {
            EquipmentPart part = InventoryRowOrder[i];
            GetOwnedEquipmentList(part).Clear();
        }

        if (_inventory == null || _inventory.Items == null)
        {
            return;
        }

        for (int i = 0; i < _inventory.Items.Count; i++)
        {
            ItemData item = _inventory.Items[i];
            if (item != null && item.IsEquipment)
            {
                if (_ownedEquipmentByPart.TryGetValue(item.equipmentPart, out List<ItemData> partItems))
                {
                    partItems.Add(item);
                }
            }
        }

        if (_equipment == null)
        {
            return;
        }

        for (int i = 0; i < InventoryRowOrder.Length; i++)
        {
            EquipmentPart part = InventoryRowOrder[i];
            ItemData equippedItem = _equipment.GetEquippedItem(part);
            if (equippedItem == null)
            {
                continue;
            }

            List<ItemData> partItems = GetOwnedEquipmentList(part);
            if (!partItems.Contains(equippedItem))
            {
                partItems.Insert(0, equippedItem);
            }
        }
    }

    private void RefreshCompletedQuestTexts()
    {
        if (_completedQuestTexts == null || _completedQuestTexts.Length == 0)
        {
            return;
        }

        IReadOnlyList<QuestData> completedQuests = QuestManager.Instance != null
            ? QuestManager.Instance.CompletedQuests
            : null;

        for (int i = 0; i < _completedQuestTexts.Length; i++)
        {
            TextMeshProUGUI text = _completedQuestTexts[i];
            if (text == null)
            {
                continue;
            }

            if (completedQuests != null && i < completedQuests.Count && completedQuests[i] != null)
            {
                text.text = completedQuests[i].QuestTitle;
            }
            else
            {
                text.text = "-";
            }
        }
    }

    private void OnSlotClicked(ItemData data)
    {
        if (data == null || _equipment == null) return;
        _equipment.EquipItem(data);
        // EquipmentChanged 이벤트 → RefreshSlots 자동 호출
    }

    private void RefreshPartSlots(EquipmentPart part)
    {
        Transform row = GetRowContainer(part);
        if (row == null)
        {
            return;
        }

        PrepareRowContainer(row);

        List<ItemData> items = GetOwnedEquipmentList(part);
        List<InventorySlotUi> slots = GetSlotList(part);
        int slotCount = Mathf.Max(_slotsPerRow, items.Count);
        EnsureSlotCount(slots, slotCount, row);

        Sprite backgroundSprite = GetBackgroundSprite(part);
        for (int i = 0; i < slots.Count; i++)
        {
            if (i < items.Count)
            {
                ItemData data = items[i];
                bool isEquipped = _equipment != null && _equipment.IsEquipped(data);
                slots[i].Setup(data, isEquipped, OnSlotClicked, backgroundSprite);
                slots[i].gameObject.SetActive(true);
            }
            else
            {
                slots[i].Setup(null, false, null, backgroundSprite);
                slots[i].gameObject.SetActive(true);
            }
        }
    }

    private void PrepareRowContainer(Transform row)
    {
        if (row == null || _preparedRows.Contains(row))
        {
            return;
        }

        for (int i = 0; i < row.childCount; i++)
        {
            Transform child = row.GetChild(i);
            if (child.GetComponent<InventorySlotUi>() == null)
            {
                child.gameObject.SetActive(false);
            }
        }

        _preparedRows.Add(row);
    }

    private void EnsureSlotCount(List<InventorySlotUi> slots, int needed, Transform parent)
    {
        while (slots.Count < needed)
        {
            InventorySlotUi slot = Instantiate(_slotPrefab, parent);
            slots.Add(slot);
        }
    }

    private List<InventorySlotUi> GetSlotList(EquipmentPart part)
    {
        if (!_slotsByPart.TryGetValue(part, out List<InventorySlotUi> slots))
        {
            slots = new List<InventorySlotUi>();
            _slotsByPart.Add(part, slots);
        }

        return slots;
    }

    private List<ItemData> GetOwnedEquipmentList(EquipmentPart part)
    {
        if (!_ownedEquipmentByPart.TryGetValue(part, out List<ItemData> items))
        {
            items = new List<ItemData>();
            _ownedEquipmentByPart.Add(part, items);
        }

        return items;
    }

    private Transform GetRowContainer(EquipmentPart part)
    {
        return part switch
        {
            EquipmentPart.Sword => _swordSlotRow,
            EquipmentPart.Shield => _shieldSlotRow,
            EquipmentPart.Helmet => _helmetSlotRow,
            EquipmentPart.Chest => _chestSlotRow,
            EquipmentPart.Legs => _legsSlotRow,
            _ => null
        };
    }

    private Sprite GetBackgroundSprite(EquipmentPart part)
    {
        return part switch
        {
            EquipmentPart.Sword => _swordSlotBackground,
            EquipmentPart.Shield => _shieldSlotBackground,
            EquipmentPart.Helmet => _helmetSlotBackground,
            EquipmentPart.Chest => _chestSlotBackground,
            EquipmentPart.Legs => _legsSlotBackground,
            _ => null
        };
    }

    public static int GetEquipmentPartOrder(EquipmentPart part)
    {
        for (int i = 0; i < InventoryRowOrder.Length; i++)
        {
            if (InventoryRowOrder[i] == part)
            {
                return i;
            }
        }

        return InventoryRowOrder.Length;
    }

    private void RefreshEquippedIcons()
    {
        CacheEquippedSlotImages();

        for (int i = 0; i < InventoryRowOrder.Length; i++)
        {
            EquipmentPart part = InventoryRowOrder[i];
            SetEquippedSlotIcon(part, _equipment != null ? _equipment.GetEquippedItem(part) : null);
        }
    }

    private void CacheEquippedSlotImages()
    {
        _equippedSlotImages[EquipmentPart.Sword] = _equippedSwordIcon;
        _equippedSlotImages[EquipmentPart.Shield] = _equippedShieldIcon;
        _equippedSlotImages[EquipmentPart.Helmet] = ResolveEquippedSlotImage(_equippedHelmetIcon, "HelmetSlot");
        _equippedSlotImages[EquipmentPart.Chest] = ResolveEquippedSlotImage(_equippedChestIcon, "ChestSlot");
        _equippedSlotImages[EquipmentPart.Legs] = ResolveEquippedSlotImage(_equippedLegsIcon, "LegSlot");

        _equippedHelmetIcon = _equippedSlotImages[EquipmentPart.Helmet];
        _equippedChestIcon = _equippedSlotImages[EquipmentPart.Chest];
        _equippedLegsIcon = _equippedSlotImages[EquipmentPart.Legs];
    }

    private void SetEquippedSlotIcon(EquipmentPart part, ItemData item)
    {
        if (!_equippedSlotImages.TryGetValue(part, out Image slotImage) || slotImage == null)
        {
            return;
        }

        Image iconImage = GetOrCreateEquippedIconImage(part, slotImage);
        if (iconImage == null)
        {
            return;
        }

        iconImage.sprite = item != null ? item.icon : null;
        iconImage.enabled = item != null && item.icon != null;
    }

    private Image GetOrCreateEquippedIconImage(EquipmentPart part, Image slotImage)
    {
        if (_equippedIconImages.TryGetValue(part, out Image iconImage) && iconImage != null)
        {
            return iconImage;
        }

        Transform child = slotImage.transform.Find(EquippedIconChildName);
        if (child != null)
        {
            iconImage = child.GetComponent<Image>();
        }

        if (iconImage == null)
        {
            GameObject iconObject = new GameObject(EquippedIconChildName, typeof(RectTransform), typeof(Image));
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.SetParent(slotImage.transform, false);
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(12f, 12f);
            iconRect.offsetMax = new Vector2(-12f, -12f);

            iconImage = iconObject.GetComponent<Image>();
            iconImage.raycastTarget = false;
            iconImage.preserveAspect = true;
        }

        _equippedIconImages[part] = iconImage;
        return iconImage;
    }

    private Image ResolveEquippedSlotImage(Image assignedImage, string fallbackObjectName)
    {
        if (assignedImage != null)
        {
            return assignedImage;
        }

        Transform root = _panelRoot != null ? _panelRoot.transform : transform;
        Transform slot = FindChildRecursive(root, fallbackObjectName);
        return slot != null ? slot.GetComponent<Image>() : null;
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
}
