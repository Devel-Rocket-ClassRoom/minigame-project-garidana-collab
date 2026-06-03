using UnityEngine;

public class NPCInteractable : MonoBehaviour, IInteractable
{
    [SerializeField]
    private string _npcName = "마을 치유사";

    [SerializeField]
    private string _interactionPrompt = "200골드로 체력 회복 충전";

    [SerializeField]    
    private int _refillCost = 200;

    public string InteractionPrompt => $"{_refillCost}골드로 체력 회복 충전";


    public Transform Transform => transform;
    public bool CanInteract(GameObject interactor)
    {
        return true;
    }

    public void Interact(GameObject interactor)
    {
        Debug.Log($"{_npcName} NPC와 상호작용 했습니다. Interactor: {interactor.name}");

        PlayerStats playerStats = interactor.GetComponent<PlayerStats>();
        PlayerHealing playerHealing = interactor.GetComponent<PlayerHealing>();

        if (playerStats == null || playerHealing == null)
        {
            return;
        }

        if (!playerStats.SpendGold(_refillCost))
        {
            Debug.Log ($"골드가 부족합니다. 필요 골드 {_refillCost}, 현재 골드: {playerStats.Gold}");
            return;
        }

        playerHealing.RefillHealItems();
        playerStats.RestoreFullHealth();
        Debug.Log($"회복 물약을 모두 충전했습니다. 사용 골드: {_refillCost}");
    }
}
