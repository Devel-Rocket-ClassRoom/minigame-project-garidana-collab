using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUi : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInventory  _inventory;
    [SerializeField] private EquipmentManager _equipment;
    [SerializeField] private PlayerInputReader _playerInputReader;
    [SerializeField] private PlayerStats _playerStats;
    [SerializeField] private PlayerAttackUpgrade _playerAttackUpgrade;

    [Header("UI")]
    // 인벤토리 전체 패널 루트 (SetActive 토글 대상)
    [SerializeField] private GameObject      _panelRoot;
    // 슬롯 버튼들이 들어갈 Grid Layout Group 부모
    [SerializeField] private Transform       _slotContainer;
    // 슬롯 하나짜리 프리팹 (Button + Image + TextMeshProUGUI)
    [SerializeField] private InventorySlotUi _slotPrefab;

    [Header("장착 슬롯 표시 (선택)")]
    [SerializeField] private Image _equippedSwordIcon;
    [SerializeField] private Image _equippedShieldIcon;

    [Header("Stat UI")]
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private TextMeshProUGUI _attackText;
    [SerializeField] private TextMeshProUGUI _hpText;
    [SerializeField] private TextMeshProUGUI _upgradeStageText;
    [SerializeField] private TextMeshProUGUI[] _completedQuestTexts;

    private readonly List<InventorySlotUi> _slots = new List<InventorySlotUi>();
    private readonly List<ItemData> _ownedEquipmentItems = new List<ItemData>();
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

        if (_inventory == null || _slotContainer == null || _slotPrefab == null) return;

        CollectOwnedEquipmentItems();
        int itemCount = _ownedEquipmentItems.Count;
        EnsureSlotCount(itemCount);

        for (int i = 0; i < _slots.Count; i++)
        {
            if (i < itemCount)
            {
                ItemData data      = _ownedEquipmentItems[i];
                bool     isEquipped = _equipment != null && _equipment.IsEquipped(data);
                _slots[i].Setup(data, isEquipped, OnSlotClicked);
                _slots[i].gameObject.SetActive(true);
            }
            else
            {
                _slots[i].gameObject.SetActive(false);
            }
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
        _ownedEquipmentItems.Clear();

        if (_inventory == null || _inventory.Items == null)
        {
            return;
        }

        for (int i = 0; i < _inventory.Items.Count; i++)
        {
            ItemData item = _inventory.Items[i];
            if (item != null && item.IsEquipment)
            {
                _ownedEquipmentItems.Add(item);
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

    private void EnsureSlotCount(int needed)
    {
        while (_slots.Count < needed)
        {
            InventorySlotUi slot = Instantiate(_slotPrefab, _slotContainer);
            _slots.Add(slot);
        }
    }

    private void RefreshEquippedIcons()
    {
        if (_equippedSwordIcon != null)
        {
            ItemData sw = _equipment?.EquippedSword;
            _equippedSwordIcon.sprite  = sw?.icon;
            _equippedSwordIcon.enabled = sw != null;
        }
        if (_equippedShieldIcon != null)
        {
            ItemData sh = _equipment?.EquippedShield;
            _equippedShieldIcon.sprite  = sh?.icon;
            _equippedShieldIcon.enabled = sh != null;
        }
    }
}
