using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUi : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInventory  _inventory;
    [SerializeField] private EquipmentManager _equipment;

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

    private readonly List<InventorySlotUi> _slots = new List<InventorySlotUi>();
    private bool _isOpen = false;

    private void Awake()
    {
        if (_inventory == null)
            _inventory = FindFirstObjectByType<PlayerInventory>();
        if (_equipment == null)
            _equipment = FindFirstObjectByType<EquipmentManager>();

        SetPanelActive(false);
    }

    private void OnEnable()
    {
        if (_inventory != null) _inventory.InventoryChanged  += RefreshSlots;
        if (_equipment != null) _equipment.EquipmentChanged  += RefreshSlots;
    }

    private void OnDisable()
    {
        if (_inventory != null) _inventory.InventoryChanged  -= RefreshSlots;
        if (_equipment != null) _equipment.EquipmentChanged  -= RefreshSlots;
    }

    // TODO: PlayerInputReader에 OnInventory 콜백 추가 후 InputSystem으로 교체
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
            TogglePanel();
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
            RefreshSlots();
    }

    private void RefreshSlots()
    {
        if (_inventory == null || _slotContainer == null) return;

        int itemCount = _inventory.Count;
        EnsureSlotCount(itemCount);

        for (int i = 0; i < _slots.Count; i++)
        {
            if (i < itemCount)
            {
                ItemData data      = _inventory.Items[i];
                bool     isEquipped = _equipment != null && _equipment.IsEquipped(data);
                _slots[i].Setup(data, isEquipped, OnSlotClicked);
                _slots[i].gameObject.SetActive(true);
            }
            else
            {
                _slots[i].gameObject.SetActive(false);
            }
        }

        RefreshEquippedIcons();
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
