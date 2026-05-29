using UnityEngine;

// CollectPrefab(worldPrefab)에 붙이는 픽업 스크립트.
// 플레이어가 닿으면 PlayerInventory에 아이템을 추가하고 자신을 제거합니다.
[RequireComponent(typeof(Collider))]
public class EquipmentItemPickup : MonoBehaviour
{
    [SerializeField]
    private ItemData _itemData;

    // 스폰 직후 플레이어와 즉시 충돌하는 것을 방지하는 딜레이
    [SerializeField]
    private float _pickupDelay = 0.3f;

    private float _spawnTime;

    private void Start()
    {
        _spawnTime = Time.time;

        // Collider는 Trigger여야 합니다.
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    // ItemData를 외부(ChestInteractable)에서 주입할 때 사용
    public void Init(ItemData data)
    {
        _itemData = data;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Time.time < _spawnTime + _pickupDelay)
            return;

        if (!other.CompareTag("Player"))
            return;

        PlayerInventory inventory = other.GetComponentInParent<PlayerInventory>();
        if (inventory == null)
            return;

        bool added = inventory.AddItem(_itemData);
        if (added)
        {
            Debug.Log($"[Pickup] {_itemData.displayName} 획득!");
            Destroy(gameObject);
        }
    }
}
