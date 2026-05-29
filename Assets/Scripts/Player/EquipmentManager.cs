using System;
using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStats     _playerStats;
    [SerializeField] private PlayerInventory _inventory;

    [Header("Weapon Mount Points")]
    // 캐릭터 Armature 안의 오른손 본 (Sword_1~6 오브젝트들의 부모)
    [SerializeField] private Transform _swordMountParent;
    // 캐릭터 Armature 안의 왼손 본 (shield_1~6 오브젝트들의 부모)
    [SerializeField] private Transform _shieldMountParent;

    [Header("Default Equipment")]
    [SerializeField] private ItemData _defaultSword;
    [SerializeField] private ItemData _defaultShield;

    public event Action EquipmentChanged;

    private ItemData _equippedSword;
    private ItemData _equippedShield;

    public ItemData EquippedSword  => _equippedSword;
    public ItemData EquippedShield => _equippedShield;

    private void Awake()
    {
        if (_playerStats == null)
            _playerStats = GetComponent<PlayerStats>();
        if (_inventory == null)
            _inventory = GetComponent<PlayerInventory>();
    }

    private void Start()
    {
        // 기본 장비 자동 장착 (Sword_1, shield_1 활성화)
        if (_defaultSword != null)  EquipItem(_defaultSword);
        if (_defaultShield != null) EquipItem(_defaultShield);
    }

    // InventoryUi 슬롯 클릭 시 호출
    public void EquipItem(ItemData data)
    {
        if (data == null || !data.IsEquipment)
        {
            Debug.LogWarning("[Equipment] 장착 불가: 장비 아이템이 아닙니다.");
            return;
        }

        switch (data.equipmentPart)
        {
            case EquipmentPart.Sword:
                SwapEquip(ref _equippedSword, data, _swordMountParent);
                break;
            case EquipmentPart.Shield:
                SwapEquip(ref _equippedShield, data, _shieldMountParent);
                break;
            default:
                Debug.LogWarning($"[Equipment] {data.displayName}: 검/방패만 지원합니다.");
                break;
        }
    }

    public void UnequipItem(EquipmentPart part)
    {
        switch (part)
        {
            case EquipmentPart.Sword:
                if (_equippedSword != null)
                {
                    RemoveStats(_equippedSword);
                    SetModelActive(_equippedSword.equipObjectName, _swordMountParent, false);
                    _equippedSword = null;
                    EquipmentChanged?.Invoke();
                }
                break;
            case EquipmentPart.Shield:
                if (_equippedShield != null)
                {
                    RemoveStats(_equippedShield);
                    SetModelActive(_equippedShield.equipObjectName, _shieldMountParent, false);
                    _equippedShield = null;
                    EquipmentChanged?.Invoke();
                }
                break;
        }
    }

    // InventoryUi에서 슬롯 하이라이트 여부 판단에 사용
    public bool IsEquipped(ItemData data)
    {
        return data != null && (data == _equippedSword || data == _equippedShield);
    }

    // ─────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────

    private void SwapEquip(ref ItemData slot, ItemData newItem, Transform mountParent)
    {
        // 1. 이전 장착 해제
        if (slot != null)
        {
            RemoveStats(slot);
            SetModelActive(slot.equipObjectName, mountParent, false);
        }

        // 2. 새 아이템 장착
        slot = newItem;
        ApplyStats(slot);
        SetModelActive(slot.equipObjectName, mountParent, true);

        EquipmentChanged?.Invoke();
        Debug.Log($"[Equipment] 장착: {newItem.displayName}");
    }

    private void ApplyStats(ItemData data)
    {
        if (_playerStats == null || data == null) return;
        if (data.attackBonus    != 0) _playerStats.IncreaseAttackPower(data.attackBonus);
        if (data.maxHealthBonus != 0) _playerStats.IncreaseMaxHealth(data.maxHealthBonus);
    }

    // 스탯 제거 시 현재 HP가 1 아래로 내려가지 않도록 보호
    private void RemoveStats(ItemData data)
    {
        if (_playerStats == null || data == null) return;
        if (data.attackBonus    != 0) _playerStats.IncreaseAttackPower(-data.attackBonus);
        if (data.maxHealthBonus != 0)
        {
            float safeRemove = Mathf.Min(data.maxHealthBonus, _playerStats.CurrentHealth - 1f);
            _playerStats.IncreaseMaxHealth(-safeRemove);
        }
    }

    private void SetModelActive(string equipName, Transform mountParent, bool active)
    {
        if (mountParent == null || string.IsNullOrEmpty(equipName)) return;

        Transform target = mountParent.Find(equipName);
        if (target != null)
        {
            target.gameObject.SetActive(active);
        }
        else
        {
            Debug.LogWarning($"[Equipment] '{equipName}'을 '{mountParent.name}' 하위에서 찾을 수 없습니다.");
        }
    }
}
