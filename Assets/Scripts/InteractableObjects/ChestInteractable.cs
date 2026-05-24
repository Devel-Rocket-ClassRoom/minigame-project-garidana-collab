using System.Collections;
using UnityEngine;

public class ChestInteractable : MonoBehaviour, IInteractable
{
    public GameObject lid;
    public GameObject body;


    private string _interactionPtompt = "Open Chest";


    [Header("Lid Open Settings")]
    public Vector3 openAngle = new Vector3(-90f, 0f, 0f); // 인스펙터에서 조절
    public float openDuration = 0.5f;

    private bool isOpen = false;
    private bool isAnimating = false;

    public string InteractionPrompt => _interactionPtompt;

    public Transform Transform => transform;

    private void Awake()
    {

    }

    private void Update()
    {
        if (isOpen || isAnimating)
            return;

        // if (Vector3.Distance(transform.position, player.transform.position) < interactableRange)
        // {
        //     if (Input.GetKeyDown(KeyCode.F))
        //     {
        //         OpenLid();
        //     }
        // }
    }

    public void OpenLid()
    {
        // isOpen/isAnimating 으로 중복방지

        if (isOpen || isAnimating)
            return;
        StartCoroutine(RotateLid());

        // OpenChestInv();
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

    // public void OpenChestInv()
    // {
    //     chestInventory.SetActive(true);
    //     Debug.Log("OpenChestInv called / chestInventory: " + chestInventory);
    // }

    // public void CloseChestInv()
    // {
    //     chestInventory.SetActive(false);
    // }

    public bool CanInteract(GameObject interactor)
    {
        return !isOpen && !isAnimating;
    }

    public void Interact(GameObject interactor)
    {
        OpenLid();
    }

    // private void GenerateRandomItemInChest()
    // {
    //     int attempts = Random.Range(1, 7);
    //     for (int i = 0; i < attempts; i++)
    //     {
    //         ItemData data = DataTableManager.ItemTable.GetRandom();
    //         if (data == null)
    //             return;

    //         itemDataList.Add(data);
    //     }
    //     foreach (var item in itemDataList)
    //     {
    //         Debug.Log(item.ItemName);
    //     }
    // }
}
