using UnityEngine;
using System.Collections.Generic;

public class MerchantInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string _merchantId;
    [SerializeField] private string _merchantName = "임용수";
    [SerializeField] private string _interactionPrompt = "상점 열기";
    [SerializeField] private ShopUi _shopUi;
    [SerializeField] private List<ItemData> _stock = new List<ItemData>();

    public string InteractionPrompt => _interactionPrompt;
    public Transform Transform => transform;
    public IReadOnlyList<ItemData> Stock => _stock;
    public string MerchantId => string.IsNullOrWhiteSpace(_merchantId)
        ? PersistenceIdUtility.BuildHierarchyId(transform, "merchant")
        : _merchantId;

    public bool RemoveStockItem(ItemData item)
    {
        if (item == null || _stock == null)
        {
            return false;
        }

        return _stock.Remove(item);
    }

    public void RestoreStock(IReadOnlyList<ItemData> items)
    {
        _stock = new List<ItemData>();

        if (items == null)
        {
            return;
        }

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null)
            {
                _stock.Add(items[i]);
            }
        }
    }

    private void Awake()
    {
#if UNITY_EDITOR
        if (_stock == null || _stock.Count == 0)
        {
            PopulateDefaultArmorStockInEditor();
        }
#endif
    }

    public bool CanInteract(GameObject interactor)
    {
        return interactor != null;
    }

    public void Interact(GameObject interactor)
    {
        if (_shopUi == null)
        {
            _shopUi = ShopUi.Ensure();
        }

        if (_shopUi == null)
        {
            Debug.LogWarning("[Merchant] ShopUi를 찾거나 생성할 수 없습니다.");
            return;
        }

        if (_shopUi.IsOpen && _shopUi.CurrentMerchant == this)
        {
            _shopUi.Close();
            return;
        }

        Debug.Log($"[{_merchantName}] 상점을 엽니다. Interactor: {interactor.name}");
        _shopUi.Open(this, interactor);
    }

#if UNITY_EDITOR
    private void Reset()
    {
        PopulateDefaultArmorStockInEditor();
    }

    private void OnValidate()
    {
        if (_stock == null || _stock.Count == 0)
        {
            PopulateDefaultArmorStockInEditor();
        }
    }

    private void PopulateDefaultArmorStockInEditor()
    {
        if (_stock == null)
        {
            _stock = new List<ItemData>();
        }

        if (_stock.Count > 0)
        {
            return;
        }

        string[] guids = UnityEditor.AssetDatabase.FindAssets(
            "t:ItemData",
            new[]
            {
                "Assets/Scripts/ScriptableObjects/Items/Helmets",
                "Assets/Scripts/ScriptableObjects/Items/Chests",
                "Assets/Scripts/ScriptableObjects/Items/Leggings",
                "Assets/Scripts/ScriptableObjects/Items/Shields"
            });

        for (int i = 0; i < guids.Length; i++)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
            ItemData item = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item != null && item.IsEquipment && item.price > 0)
            {
                _stock.Add(item);
            }
        }

        _stock.Sort((a, b) =>
        {
            int partOrder = InventoryUi.GetEquipmentPartOrder(a.equipmentPart)
                .CompareTo(InventoryUi.GetEquipmentPartOrder(b.equipmentPart));

            return partOrder != 0 ? partOrder : a.tier.CompareTo(b.tier);
        });
    }
#endif
}
