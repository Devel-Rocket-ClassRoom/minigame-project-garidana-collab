using UnityEngine;

public class NPCInteractable : MonoBehaviour, IInteractable
{
    [SerializeField]
    private string _npcName = "마을 치유사";

    [SerializeField]    
    private int _refillCost = 200;

    [SerializeField]
    private int _refillCostIncreasePer10Levels = 100;

    private int _currentRefillCost;

    public string InteractionPrompt => $"{_currentRefillCost}골드로 체력 회복 충전";

    public Transform Transform => transform;

    private void Awake()
    {
        _currentRefillCost = _refillCost;
    }

    public bool CanInteract(GameObject interactor)
    {
        PlayerStats playerStats = interactor.GetComponent<PlayerStats>();
        _currentRefillCost = GetCurrentRefillCost(playerStats);
        return true;
    }

    public void Interact(GameObject interactor)
    {
        Debug.Log($"{_npcName} NPC와 상호작용 했습니다. Interactor: {interactor.name}");

        PlayerStats playerStats = interactor.GetComponent<PlayerStats>();
        PlayerHealing playerHealing = interactor.GetComponent<PlayerHealing>();
        PlayerRewardFloatingText playerFloatingText = interactor.GetComponent<PlayerRewardFloatingText>();

        if (playerStats == null || playerHealing == null)
        {
            return;
        }

        int refillCost = GetCurrentRefillCost(playerStats);

        if (!playerStats.SpendGold(refillCost))
        {
            playerFloatingText?.ShowNotice("골드 부족", new Color(1f, 0.35f, 0.2f));
            Debug.Log ($"골드가 부족합니다. 필요 골드 {refillCost}, 현재 골드: {playerStats.Gold}");
            return;
        }

        playerHealing.RefillHealItems();
        playerStats.RestoreFullHealth();
        playerFloatingText?.ShowNotice("체력 회복 충전", new Color(0.2f, 1f, 0.45f));
        Debug.Log($"회복 물약을 모두 충전했습니다. 사용 골드: {refillCost}");
    }

    private int GetCurrentRefillCost(PlayerStats playerStats)
    {
        if (playerStats == null)
        {
            return _refillCost;
        }

        int levelBonusSteps = Mathf.Max(0, playerStats.Level / 10);
        return _refillCost + (levelBonusSteps * _refillCostIncreasePer10Levels);
    }
}
