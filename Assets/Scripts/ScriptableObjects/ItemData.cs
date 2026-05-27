using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Scriptable Objects/ItemData")]
public class ItemData : ScriptableObject
{
    [Header("Identity")]
    public string itemId;
    public string displayName;
    [TextArea]
    public string description;

    [Header("Classification")]
    public ItemType itemType;
    public EquipmentPart equipmentPart;
    public int tier = 1;

    [Header("Stats")]
    public float attackBonus;
    public float maxHealthBonus;

    [Header("Economy")]
    public int price;

    [Header("Visuals")]
    public Sprite icon;
    public GameObject worldPrefab;
    public string equipObjectName;

    [Header("Rules")]
    public bool isDefaultEquipment;

    public bool IsEquipment => itemType == ItemType.Equipment;

    private void OnValidate()
    {
        tier = Mathf.Max(1, tier);
        attackBonus = Mathf.Max(0f, attackBonus);
        maxHealthBonus = Mathf.Max(0f, maxHealthBonus);
        price = Mathf.Max(0, price);
    }
}

public enum ItemType
{
    Quest,
    Equipment,
    Consumable,
    Material
}

public enum EquipmentPart
{
    None,
    Sword,
    Shield,
    Helmet,
    Chest,
    Legs
}
