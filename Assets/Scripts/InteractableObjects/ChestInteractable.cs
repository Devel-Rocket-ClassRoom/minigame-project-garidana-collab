using System.Collections;
using UnityEngine;

public class ChestInteractable : MonoBehaviour, IInteractable
{
    public GameObject lid;
    public GameObject body;

    private string _interactionPtompt = "Open Chest";

    [Header("Lid Open Settings")]
    public Vector3 openAngle = new Vector3(-90f, 0f, 0f);
    public float openDuration = 0.5f;

    [Header("Drop Settings")]
    [SerializeField] private DropEntry[] _dropTable;
    // 아이템이 스폰될 오프셋 (상자 위쪽)
    [SerializeField] private Vector3 _spawnOffset = new Vector3(0f, 0.5f, 0f);
    // 스폰 후 튀어오르는 힘
    [SerializeField] private float _popForce = 3f;

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
        OpenLid();
        RollDrops();
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

    private void RollDrops()
    {
        if (_dropTable == null || _dropTable.Length == 0)
            return;

        foreach (DropEntry entry in _dropTable)
        {
            if (entry.item == null)
                continue;

            if (Random.value <= entry.dropChance)
                SpawnItem(entry.item);
        }
    }

    private void SpawnItem(ItemData itemData)
    {
        if (itemData.worldPrefab == null)
        {
            Debug.LogWarning($"[Chest] {itemData.displayName}의 worldPrefab이 없습니다.");
            return;
        }

        Vector3 spawnPos = transform.position + _spawnOffset;
        GameObject go = Instantiate(itemData.worldPrefab, spawnPos, Quaternion.identity);

        // EquipmentItemPickup 스크립트에 ItemData 주입
        EquipmentItemPickup pickup = go.GetComponent<EquipmentItemPickup>();
        if (pickup != null)
            pickup.Init(itemData);
        else
            Debug.LogWarning($"[Chest] {go.name}에 EquipmentItemPickup 컴포넌트가 없습니다.");

        // 위로 튀어오르는 물리 효과
        Rigidbody rb = go.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 popDir = (Vector3.up + Random.insideUnitSphere * 0.3f).normalized;
            rb.AddForce(popDir * _popForce, ForceMode.Impulse);
        }

        Debug.Log($"[Chest] {itemData.displayName} 스폰됨");
    }
}
