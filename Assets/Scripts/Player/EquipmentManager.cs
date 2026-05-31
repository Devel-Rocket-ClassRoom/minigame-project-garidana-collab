using System;
using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStats     _playerStats;
    [SerializeField] private PlayerInventory _inventory;

    [Header("Equipment Mount Points")]
    [SerializeField] private Transform _swordMountParent;
    [SerializeField] private Transform _shieldMountParent;
    [SerializeField] private Transform _helmetMountParent;
    [SerializeField] private Transform _chestMountParent;
    [SerializeField] private Transform _legsMountParent;

    [Header("Default Equipment")]
    [SerializeField] private ItemData _defaultSword;
    [SerializeField] private ItemData _defaultShield;
    [SerializeField] private ItemData _defaultHelmet;
    [SerializeField] private ItemData _defaultChest;
    [SerializeField] private ItemData _defaultLegs;

    public event Action EquipmentChanged;

    private ItemData _equippedSword;
    private ItemData _equippedShield;
    private ItemData _equippedHelmet;
    private ItemData _equippedChest;
    private ItemData _equippedLegs;

    public ItemData EquippedSword  => _equippedSword;
    public ItemData EquippedShield => _equippedShield;
    public ItemData EquippedHelmet => _equippedHelmet;
    public ItemData EquippedChest  => _equippedChest;
    public ItemData EquippedLegs   => _equippedLegs;

    private void Awake()
    {
        if (_playerStats == null)
            _playerStats = GetComponent<PlayerStats>();
        if (_inventory == null)
            _inventory = GetComponent<PlayerInventory>();
    }

    private void Start()
    {
        EquipDefaultItem(_defaultSword);
        EquipDefaultItem(_defaultShield);
        EquipDefaultItem(_defaultHelmet);
        EquipDefaultItem(_defaultChest);
        EquipDefaultItem(_defaultLegs);
    }

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
                SwapEquip(ref _equippedSword, data, GetMountParent(EquipmentPart.Sword));
                break;
            case EquipmentPart.Shield:
                SwapEquip(ref _equippedShield, data, GetMountParent(EquipmentPart.Shield));
                break;
            case EquipmentPart.Helmet:
                SwapEquip(ref _equippedHelmet, data, GetMountParent(EquipmentPart.Helmet));
                break;
            case EquipmentPart.Chest:
                SwapEquip(ref _equippedChest, data, GetMountParent(EquipmentPart.Chest));
                break;
            case EquipmentPart.Legs:
                SwapEquip(ref _equippedLegs, data, GetMountParent(EquipmentPart.Legs));
                break;
            default:
                Debug.LogWarning($"[Equipment] 지원하지 않는 장비 부위입니다: {data.displayName}");
                break;
        }
    }

    public void UnequipItem(EquipmentPart part)
    {
        switch (part)
        {
            case EquipmentPart.Sword:
                Unequip(ref _equippedSword, EquipmentPart.Sword);
                break;
            case EquipmentPart.Shield:
                Unequip(ref _equippedShield, EquipmentPart.Shield);
                break;
            case EquipmentPart.Helmet:
                Unequip(ref _equippedHelmet, EquipmentPart.Helmet);
                break;
            case EquipmentPart.Chest:
                Unequip(ref _equippedChest, EquipmentPart.Chest);
                break;
            case EquipmentPart.Legs:
                Unequip(ref _equippedLegs, EquipmentPart.Legs);
                break;
        }
    }

    public bool IsEquipped(ItemData data)
    {
        return data != null &&
               (data == _equippedSword ||
                data == _equippedShield ||
                data == _equippedHelmet ||
                data == _equippedChest ||
                data == _equippedLegs);
    }

    public ItemData GetEquippedItem(EquipmentPart part)
    {
        return part switch
        {
            EquipmentPart.Sword => _equippedSword,
            EquipmentPart.Shield => _equippedShield,
            EquipmentPart.Helmet => _equippedHelmet,
            EquipmentPart.Chest => _equippedChest,
            EquipmentPart.Legs => _equippedLegs,
            _ => null
        };
    }

    private void SwapEquip(ref ItemData slot, ItemData newItem, Transform mountParent)
    {
        if (slot != null)
        {
            RemoveStats(slot);
            SetModelActive(slot.equipObjectName, mountParent, false);
        }

        slot = newItem;
        ApplyStats(slot);
        SetModelActive(slot.equipObjectName, mountParent, true);

        EquipmentChanged?.Invoke();
        Debug.Log($"[Equipment] 장착: {newItem.displayName}");
    }

    private void EquipDefaultItem(ItemData data)
    {
        if (data == null)
        {
            return;
        }

        if (_inventory != null && !_inventory.HasItem(data))
        {
            _inventory.AddItem(data);
        }

        EquipItem(data);
    }

    private void Unequip(ref ItemData slot, EquipmentPart part)
    {
        if (slot == null)
        {
            return;
        }

        RemoveStats(slot);
        SetModelActive(slot.equipObjectName, GetMountParent(part), false);
        slot = null;
        EquipmentChanged?.Invoke();
    }

    private void ApplyStats(ItemData data)
    {
        if (_playerStats == null || data == null) return;
        if (data.attackBonus    != 0) _playerStats.IncreaseAttackPower(data.attackBonus);
        if (data.maxHealthBonus != 0) _playerStats.IncreaseMaxHealth(data.maxHealthBonus);
    }

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
        if (string.IsNullOrEmpty(equipName))
        {
            return;
        }

        Transform searchRoot = mountParent != null ? mountParent : transform;
        Transform target = searchRoot.Find(equipName);
        if (target == null)
        {
            target = FindChildRecursive(searchRoot, equipName);
        }

        if (target == null && searchRoot != transform)
        {
            target = FindChildRecursive(transform, equipName);
        }

        if (target != null)
        {
            target.gameObject.SetActive(active);
        }
        else
        {
            string rootName = searchRoot != null ? searchRoot.name : name;
            Debug.LogWarning($"[Equipment] '{equipName}'을 '{rootName}' 하위에서 찾을 수 없습니다.");
        }
    }

    private Transform GetMountParent(EquipmentPart part)
    {
        return part switch
        {
            EquipmentPart.Sword => _swordMountParent,
            EquipmentPart.Shield => _shieldMountParent,
            EquipmentPart.Helmet => _helmetMountParent,
            EquipmentPart.Chest => _chestMountParent,
            EquipmentPart.Legs => _legsMountParent,
            _ => null
        };
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }

            Transform found = FindChildRecursive(child, childName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
