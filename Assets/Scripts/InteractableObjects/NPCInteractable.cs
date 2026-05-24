using UnityEngine;

public class NPCInteractable : MonoBehaviour, IInteractable
{
    [SerializeField]
    private string _npcName = "Test NPC";

    [SerializeField]
    private string _interactionPrompt = "Interact";

    public string InteractionPrompt => _interactionPrompt;

    public Transform Transform => transform;
    public bool CanInteract(GameObject interactor)
    {
        return true;
    }

    public void Interact(GameObject interactor)
    {
        Debug.Log($"{_npcName} NPC와 상호작용 했습니다. Interactor: {interactor.name}");
    }
}
