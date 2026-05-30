using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField]
    [Range(1, 40)]
    private int _maxSlots = 20;

    // InventoryUi가 구독해서 슬롯을 다시 그립니다.
    public event Action InventoryChanged;

    private readonly List<ItemData> _items = new List<ItemData>();

    public IReadOnlyList<ItemData> Items => _items;
    public bool IsFull => _items.Count >= _maxSlots;
    public int Count => _items.Count;

    // 성공하면 true, 인벤토리가 꽉 찼으면 false 반환
    public bool AddItem(ItemData data)
    {
        if (data == null || IsFull)
        {
            Debug.Log($"[Inventory] 추가 실패: data={data?.displayName}, full={IsFull}");
            return false;
        }

        _items.Add(data);
        InventoryChanged?.Invoke();

        Debug.Log($"[Inventory] 추가: {data.displayName} ({_items.Count}/{_maxSlots})");
        return true;
    }

    public bool RemoveItem(ItemData data)
    {
        bool removed = _items.Remove(data);
        if (removed)
        {
            InventoryChanged?.Invoke();
            Debug.Log($"[Inventory] 제거: {data.displayName}");
        }
        return removed;
    }

    public bool HasItem(ItemData data) => _items.Contains(data);
}
