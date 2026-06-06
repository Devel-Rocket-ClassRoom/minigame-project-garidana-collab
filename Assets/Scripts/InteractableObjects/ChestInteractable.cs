using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ChestInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string _chestId;
    public GameObject lid;
    public GameObject body;

    private string _interactionPtompt = "상자 열기";

    [Header("Lid Open Settings")]
    public Vector3 openAngle = new Vector3(-90f, 0f, 0f);
    public float openDuration = 0.5f;

    [Header("Nearby Sound")]
    [SerializeField, Min(0.1f)]
    private float _nearbySoundRadius = 5f;
    [SerializeField, Min(0.1f)]
    private float _nearbySoundInterval = 3f;

    [Header("Reward Settings")]
    [SerializeField]
    private ItemData[] _rewardItems;
    [SerializeField]
    private Vector3 _spawnOffset = new Vector3 (0f, 0.5f, 0f);

    private bool isOpen = false;
    private bool isAnimating = false;
    private ChestProximitySoundTrigger _proximitySoundTrigger;

    public string InteractionPrompt => _interactionPtompt;
    public Transform Transform => transform;
    public string ChestId => string.IsNullOrWhiteSpace(_chestId)
        ? PersistenceIdUtility.BuildHierarchyId(transform, "chest")
        : _chestId;
    public bool IsOpen => isOpen;
    public ItemData[] RewardItems => _rewardItems;

    private void Awake()
    {
        GameObject triggerObject = new GameObject("ChestNearbySoundTrigger");
        triggerObject.layer = 0;
        triggerObject.transform.SetParent(transform, false);

        SphereCollider triggerCollider = triggerObject.AddComponent<SphereCollider>();
        triggerCollider.isTrigger = true;
        triggerCollider.radius = _nearbySoundRadius;

        _proximitySoundTrigger = triggerObject.AddComponent<ChestProximitySoundTrigger>();
        _proximitySoundTrigger.Initialize(this, _nearbySoundInterval);
    }

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
        SoundManager.Instance?.PlaySFX(SoundManager.SFXType.ChestOpen);
        _proximitySoundTrigger?.SetChestAvailable(false);
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

    public void RestoreOpenedState(bool opened)
    {
        isOpen = opened;
        isAnimating = false;
        _proximitySoundTrigger?.SetChestAvailable(!opened);

        if (lid == null)
        {
            return;
        }

        if (opened)
        {
            lid.transform.localRotation = Quaternion.Euler(openAngle);
            return;
        }

        lid.transform.localRotation = Quaternion.identity;
    }
}

internal sealed class ChestProximitySoundTrigger : MonoBehaviour
{
    private static readonly HashSet<ChestProximitySoundTrigger> ActiveTriggers = new();

    private readonly HashSet<Collider> _playerColliders = new();
    private ChestInteractable _chest;
    private bool _isChestAvailable;
    private float _soundInterval;

    public void Initialize(ChestInteractable chest, float soundInterval)
    {
        _chest = chest;
        _isChestAvailable = chest != null && chest.CanInteract(null);
        _soundInterval = soundInterval;
    }

    public void SetChestAvailable(bool available)
    {
        _isChestAvailable = available;
        RefreshRegistration();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayerCollider(other))
        {
            return;
        }

        _playerColliders.Add(other);
        RefreshRegistration();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!_playerColliders.Remove(other))
        {
            return;
        }

        RefreshRegistration();
    }

    private void OnDisable()
    {
        _playerColliders.Clear();
        Unregister();
    }

    private void RefreshRegistration()
    {
        if (_isChestAvailable && _playerColliders.Count > 0)
        {
            Register();
            return;
        }

        Unregister();
    }

    private void Register()
    {
        if (!ActiveTriggers.Add(this))
        {
            return;
        }

        SoundManager.Instance?.PlayLoopSFX(
            SoundManager.SFXType.ChestNearbyLoop,
            _soundInterval);
    }

    private void Unregister()
    {
        if (!ActiveTriggers.Remove(this) || ActiveTriggers.Count > 0)
        {
            return;
        }

        SoundManager.Instance?.StopLoopSFX(SoundManager.SFXType.ChestNearbyLoop);
    }

    private static bool IsPlayerCollider(Collider other)
    {
        return other != null && other.GetComponentInParent<PlayerStats>() != null;
    }
}
