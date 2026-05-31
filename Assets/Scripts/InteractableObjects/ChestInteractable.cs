using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class ChestInteractable : MonoBehaviour, IInteractable
{
    public GameObject lid;
    public GameObject body;

    private string _interactionPtompt = "Open Chest";

    [Header("Lid Open Settings")]
    public Vector3 openAngle = new Vector3(-90f, 0f, 0f);
    public float openDuration = 0.5f;

    [Header("Reward Settings")]
    [SerializeField]
    private ItemData[] _rewardItems;
    [SerializeField]
    private Vector3 _spawnOffset = new Vector3 (0f, 0.5f, 0f);

    private bool isOpen = false;
    private bool isAnimating = false;

    public string InteractionPrompt => _interactionPtompt;
    public Transform Transform => transform;

    public bool CanInteract(GameObject interactor)
    {
        return !isOpen && !isAnimating;
    }

    public void Interact(GameObject interactor)
    {
        if (!CanInteract(interactor))
        {
            return;
        }

        PlayerInventory inventory = interactor.GetComponentInParent<PlayerInventory>();
        if (inventory == null)
        {
            Debug.LogWarning("[Chest] PlayerInventory를 찾을 수 없습니다.");
            return;
        }

        OpenLid();
        SpawnRewards(interactor.transform, inventory);
    }

    public void OpenLid()
    {
        if (isOpen || isAnimating)
            return;
        StartCoroutine(RotateLid());
    }

    private IEnumerator RotateLid()
    {
        isAnimating = true;

        Quaternion startRotation = lid.transform.localRotation;
        Quaternion endRotation = Quaternion.Euler(openAngle);
        float elapsed = 0f;

        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            lid.transform.localRotation = Quaternion.Lerp(
                startRotation,
                endRotation,
                elapsed / openDuration
            );
            yield return null;
        }

        lid.transform.localRotation = endRotation;
        isOpen = true;
        isAnimating = false;
    }

    private void SpawnRewards(Transform player, PlayerInventory inventory)
    {
        if (_rewardItems == null || _rewardItems.Length == 0)
        {
            Debug.LogWarning($"[Chest]  {name}에 연결된 보상 아이템이 없습니다.");
            return;
        }

        for (int i = 0; i < _rewardItems.Length; i++)
        {
            ItemData item = _rewardItems[i];
            if (item == null)
            {
                continue;
            }

            SpawnRewardItem(item, player, inventory, i);
        }
    }

    private void SpawnRewardItem(ItemData itemData, Transform player, PlayerInventory inventory, int index)
    {
        if (itemData.worldPrefab == null)
        {
            Debug.LogWarning ($"[Chest] {itemData.displayName}의 worldPrefab이 없습니다.");
            return;
        }

        if (inventory.HasItem(itemData))
        {
            Debug.Log ($"[Chest] 이미 보유 중인 장비입니다: {itemData.displayName}");
        }

        Vector3 spreadOffset = new Vector3 ((index - (_rewardItems.Length - 1) * 0.5f) * 0.35f, 0f , 0f);
        Vector3 spawnPosition = transform.position + _spawnOffset + spreadOffset;

        GameObject spawned = Instantiate(itemData.worldPrefab, spawnPosition, Quaternion.identity);

        EquipmentCollectEffect collectEffect = spawned.GetComponent<EquipmentCollectEffect>();
        if (collectEffect == null)
        {
            Debug.LogWarning ($"[Chest] {spawned.name}에 worldPrefab에 EquipmentCollectEffect가 없습니다.");
            Destroy(spawned);
            return;
        }

        collectEffect.Initialize(itemData, player,inventory);
    }
}
