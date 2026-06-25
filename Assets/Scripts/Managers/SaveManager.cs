using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Firebase.Database;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class SaveData
{
    public int version = 1;
    public PlayerSaveData player = new PlayerSaveData();
    public string[] inventoryItemIds = Array.Empty<string>();
    public EquipmentSaveData equipment = new EquipmentSaveData();
    public QuestSaveData quest = new QuestSaveData();
    public WaypointSaveData waypoint = new WaypointSaveData();
    public TutorialSaveData tutorial = new TutorialSaveData();
    public MerchantSaveData[] merchants = Array.Empty<MerchantSaveData>();
    public string[] openedChestIds = Array.Empty<string>();
}

[Serializable]
public class PlayerSaveData
{
    public int level = 1;
    public int currentExp;
    public int gold;
    public float currentHealth = 100f;
    public int healItemCount;
}

[Serializable]
public class EquipmentSaveData
{
    public string swordItemId;
    public string shieldItemId;
    public string helmetItemId;
    public string chestItemId;
    public string legsItemId;
}

[Serializable]
public class QuestSaveData
{
    public string currentQuestId;
    public int currentAmount;
    public string[] completedQuestIds = Array.Empty<string>();
}

[Serializable]
public class WaypointSaveData
{
    public string lastActivatedWaypointId;
    public string[] unlockedWaypointIds = Array.Empty<string>();
}

[Serializable]
public class TutorialSaveData
{
    public bool townTutorialCompleted;
    public bool chestTutorialCompleted;
}

[Serializable]
public class MerchantSaveData
{
    public string merchantId;
    public string[] remainingStockItemIds = Array.Empty<string>();
}

[Serializable]
public class CloudSavePayload
{
    public int schemaVersion = 1;
    public long updatedAt;
    public SaveData data = new SaveData();
}

public static class PersistenceIdUtility
{
    public static string BuildHierarchyId(Transform target, string prefix)
    {
        if (target == null)
        {
            return prefix;
        }

        string path = target.name;
        Transform current = target.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return $"{prefix}:{path}";
    }
}

public class SaveManager : MonoBehaviour
{
    private const string SaveRootPath = "users";
    private const string SaveNodePath = "save";


    public static SaveManager Instance { get; private set; }
    public bool IsApplyingSave => _isApplyingSave;

    private SaveData _saveData;
    private bool _hasCloudSave;
    private bool _isApplyingSave;
    private bool _isCloudBusy;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject go = new GameObject("[SaveManager]");
        Instance = go.AddComponent<SaveManager>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        _saveData = null;
        _hasCloudSave = false;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    public void SaveGame()
    {
        SaveGameAsync().Forget();
    }

    public async UniTask<bool> SaveGameAsync()
    {
        if (_isApplyingSave || _isCloudBusy)
        {
            return false;
        }

        SaveData captured = CaptureCurrentState();
        if (captured == null)
        {
            Debug.LogWarning("[Save] 현재 씬 상태를 캡처하지 못했습니다.");
            return false;
        }

        if (!CanUseCloudSave(out string error))
        {
            Debug.LogWarning($"[Save] Firebase 저장 불가: {error}");
            return false;
        }

        _isCloudBusy = true;

        try
        {
            CloudSavePayload payload = new CloudSavePayload
            {
                schemaVersion = 1,
                updatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                data = captured
            };

            string json = JsonUtility.ToJson(payload);
            await GetSaveReference().SetRawJsonValueAsync(json);
            _saveData = captured;
            _hasCloudSave = true;
            Debug.Log("[Save] Firebase 저장 완료");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Save] Firebase 저장 실패: {ex.Message}");
            return false;
        }
        finally
        {
            _isCloudBusy = false;
        }
    }

    public async UniTask<bool> LoadCloudSaveAsync()
    {
        if (!CanUseCloudSave(out string error))
        {
            Debug.LogWarning($"[Save] Firebase 로드 불가: {error}");
            _saveData = null;
            _hasCloudSave = false;
            return false;
        }

        try
        {
            DataSnapshot snapshot = await GetSaveReference().GetValueAsync();
            if (!snapshot.Exists)
            {
                if (await TryImportLegacyLocalSaveAsync())
                {
                    return true;
                }

                _saveData = null;
                _hasCloudSave = false;
                Debug.Log("[Save] Firebase 세이브 없음");
                return false;
            }

            string json = snapshot.GetRawJsonValue();
            CloudSavePayload payload = JsonUtility.FromJson<CloudSavePayload>(json);
            _saveData = payload != null ? payload.data : null;
            _hasCloudSave = _saveData != null;
            Debug.Log(_hasCloudSave ? "[Save] Firebase 세이브 로드 완료" : "[Save] Firebase 세이브 데이터가 비어 있습니다.");
            return _hasCloudSave;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Save] Firebase 로드 실패: {ex.Message}");
            _saveData = null;
            _hasCloudSave = false;
            return false;
        }
    }

    private async UniTask<bool> TryImportLegacyLocalSaveAsync()
    {
        string importedPrefsKey = LegacyLocalSaveImporter.GetImportedPrefsKey(AuthManager.Instance.UserId);
        if (PlayerPrefs.GetInt(importedPrefsKey, 0) == 1)
        {
            return false;
        }

        if (!LegacyLocalSaveImporter.HasLegacyLocalSave())
        {
            return false;
        }

        if (!LegacyLocalSaveImporter.TryLoadLegacySave(out SaveData legacySaveData, out string importError))
        {
            Debug.LogWarning($"[Save] 기존 로컬 세이브 가져오기 실패: {importError}");
            return false;
        }

        try
        {
            CloudSavePayload payload = new CloudSavePayload
            {
                schemaVersion = 1,
                updatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                data = legacySaveData
            };

            string json = JsonUtility.ToJson(payload);
            await GetSaveReference().SetRawJsonValueAsync(json);
            _saveData = legacySaveData;
            _hasCloudSave = true;
            PlayerPrefs.SetInt(importedPrefsKey, 1);
            PlayerPrefs.Save();
            Debug.Log("[Save] 기존 로컬 세이브를 Firebase로 가져왔습니다.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Save] 기존 로컬 세이브 Firebase 업로드 실패: {ex.Message}");
            return false;
        }
    }
    public bool HasSaveData()
    {
        return _hasCloudSave;
    }

    public bool IsTownTutorialCompleted => _saveData != null
        && _saveData.tutorial != null
        && _saveData.tutorial.townTutorialCompleted;

    public bool IsChestTutorialCompleted => _saveData != null
        && ((_saveData.tutorial != null && _saveData.tutorial.chestTutorialCompleted)
            || (_saveData.openedChestIds != null && _saveData.openedChestIds.Length > 0));

    public void DeleteSaveFile()
    {
        DeleteCloudSaveAsync().Forget();
    }

    public async UniTask<bool> DeleteCloudSaveAsync()
    {
        if (!CanUseCloudSave(out string error))
        {
            Debug.LogWarning($"[Save] Firebase 삭제 불가: {error}");
            _saveData = null;
            _hasCloudSave = false;
            return false;
        }

        try
        {
            await GetSaveReference().RemoveValueAsync();
            _saveData = null;
            _hasCloudSave = false;
            Debug.Log("[Save] Firebase 세이브 삭제 완료");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Save] Firebase 세이브 삭제 실패: {ex.Message}");
            return false;
        }
    }

    private bool CanUseCloudSave(out string error)
    {
        if (FirebaseInitializer.Instance == null || !FirebaseInitializer.Instance.IsReady)
        {
            error = "Firebase 초기화가 완료되지 않았습니다.";
            return false;
        }

        if (AuthManager.Instance == null || !AuthManager.Instance.IsLoggedIn)
        {
            error = "로그인이 필요합니다.";
            return false;
        }

        error = null;
        return true;
    }

    private DatabaseReference GetSaveReference()
    {
        return FirebaseInitializer.Instance.Database.RootReference
            .Child(SaveRootPath)
            .Child(AuthManager.Instance.UserId)
            .Child(SaveNodePath);
    }
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_saveData == null || scene.name != "SampleScene")
        {
            return;
        }

        StartCoroutine(ApplyLoadedStateNextFrame());
    }

    private IEnumerator ApplyLoadedStateNextFrame()
    {
        yield return null;

        if (_saveData == null)
        {
            yield break;
        }

        PlayerStats playerStats = FindFirstObjectByType<PlayerStats>();
        PlayerHealing playerHealing = FindFirstObjectByType<PlayerHealing>();
        PlayerInventory playerInventory = FindFirstObjectByType<PlayerInventory>();
        EquipmentManager equipmentManager = FindFirstObjectByType<EquipmentManager>();
        QuestManager questManager = QuestManager.Instance;
        WaypointManager waypointManager = WaypointManager.Instance;

        if (playerStats == null || playerHealing == null || playerInventory == null || equipmentManager == null || questManager == null || waypointManager == null)
        {
            Debug.LogWarning("[Save] 로드에 필요한 주요 컴포넌트를 찾지 못했습니다.");
            yield break;
        }

        _isApplyingSave = true;

        Dictionary<string, ItemData> itemRegistry = BuildItemRegistry(equipmentManager);
        Dictionary<string, QuestData> questRegistry = BuildQuestRegistry();

        ApplyMerchantState(itemRegistry);
        ApplyChestState();
        ApplyTutorialState();
        waypointManager.RestoreState(_saveData.waypoint.unlockedWaypointIds, _saveData.waypoint.lastActivatedWaypointId);

        equipmentManager.ClearAllEquippedItems();
        playerInventory.ClearItems();
        playerStats.RestoreProgress(_saveData.player.level, _saveData.player.currentExp, _saveData.player.gold);
        playerHealing.SetHealItemCount(_saveData.player.healItemCount);

        RestoreInventory(playerInventory, itemRegistry, _saveData.inventoryItemIds);
        RestoreEquipment(equipmentManager, itemRegistry, _saveData.equipment);
        playerStats.SetCurrentHealth(_saveData.player.currentHealth);
        questManager.RestoreState(
            FindQuest(questRegistry, _saveData.quest.currentQuestId),
            _saveData.quest.currentAmount,
            ResolveQuests(questRegistry, _saveData.quest.completedQuestIds));

        Transform respawnPoint = waypointManager.GetRespawnPoint();
        if (respawnPoint != null)
        {
            playerStats.TeleportTo(respawnPoint.position);
        }

        _isApplyingSave = false;
        Debug.Log("[Save] 로드 완료");
    }

    private SaveData CaptureCurrentState()
    {
        PlayerStats playerStats = FindFirstObjectByType<PlayerStats>();
        PlayerHealing playerHealing = FindFirstObjectByType<PlayerHealing>();
        PlayerInventory playerInventory = FindFirstObjectByType<PlayerInventory>();
        EquipmentManager equipmentManager = FindFirstObjectByType<EquipmentManager>();
        QuestManager questManager = QuestManager.Instance;
        WaypointManager waypointManager = WaypointManager.Instance;

        if (playerStats == null || playerHealing == null || playerInventory == null || equipmentManager == null || questManager == null || waypointManager == null)
        {
            return null;
        }

        SaveData data = new SaveData();
        data.player.level = playerStats.Level;
        data.player.currentExp = playerStats.CurrentExp;
        data.player.gold = playerStats.Gold;
        data.player.currentHealth = playerStats.CurrentHealth;
        data.player.healItemCount = playerHealing.HealItemCount;
        data.inventoryItemIds = CaptureInventoryItemIds(playerInventory);
        data.equipment = CaptureEquipment(equipmentManager);
        data.quest.currentQuestId = questManager.CurrentQuest != null ? questManager.CurrentQuest.QuestId : null;
        data.quest.currentAmount = questManager.CurrentAmount;
        data.quest.completedQuestIds = questManager.GetCompletedQuestIds();
        data.waypoint.lastActivatedWaypointId = waypointManager.LastActivatedWaypointId;
        data.waypoint.unlockedWaypointIds = CaptureWaypointIds(waypointManager);
        data.tutorial.townTutorialCompleted = CaptureTownTutorialCompleted();
        data.merchants = CaptureMerchants();
        data.openedChestIds = CaptureOpenedChestIds();
        return data;
    }

    private static string[] CaptureInventoryItemIds(PlayerInventory inventory)
    {
        List<string> itemIds = new List<string>();
        for (int i = 0; i < inventory.Items.Count; i++)
        {
            ItemData item = inventory.Items[i];
            if (item != null && !string.IsNullOrWhiteSpace(item.itemId))
            {
                itemIds.Add(item.itemId);
            }
        }

        return itemIds.ToArray();
    }

    private static EquipmentSaveData CaptureEquipment(EquipmentManager equipmentManager)
    {
        return new EquipmentSaveData
        {
            swordItemId = GetItemId(equipmentManager.EquippedSword),
            shieldItemId = GetItemId(equipmentManager.EquippedShield),
            helmetItemId = GetItemId(equipmentManager.EquippedHelmet),
            chestItemId = GetItemId(equipmentManager.EquippedChest),
            legsItemId = GetItemId(equipmentManager.EquippedLegs)
        };
    }

    private static string[] CaptureWaypointIds(WaypointManager waypointManager)
    {
        List<string> ids = new List<string>();
        foreach (string waypointId in waypointManager.UnlockedWaypointIds)
        {
            if (!string.IsNullOrWhiteSpace(waypointId))
            {
                ids.Add(waypointId);
            }
        }

        return ids.ToArray();
    }

    private static MerchantSaveData[] CaptureMerchants()
    {
        MerchantInteractable[] merchants = FindObjectsByType<MerchantInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        List<MerchantSaveData> merchantDataList = new List<MerchantSaveData>(merchants.Length);

        for (int i = 0; i < merchants.Length; i++)
        {
            MerchantInteractable merchant = merchants[i];
            if (merchant == null)
            {
                continue;
            }

            List<string> remainingStockItemIds = new List<string>();
            IReadOnlyList<ItemData> stock = merchant.Stock;
            for (int itemIndex = 0; itemIndex < stock.Count; itemIndex++)
            {
                ItemData item = stock[itemIndex];
                if (item != null && !string.IsNullOrWhiteSpace(item.itemId))
                {
                    remainingStockItemIds.Add(item.itemId);
                }
            }

            merchantDataList.Add(new MerchantSaveData
            {
                merchantId = merchant.MerchantId,
                remainingStockItemIds = remainingStockItemIds.ToArray()
            });
        }

        return merchantDataList.ToArray();
    }

    private static string[] CaptureOpenedChestIds()
    {
        ChestInteractable[] chests = FindObjectsByType<ChestInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        List<string> openedChestIds = new List<string>();

        for (int i = 0; i < chests.Length; i++)
        {
            ChestInteractable chest = chests[i];
            if (chest != null && chest.IsOpen)
            {
                openedChestIds.Add(chest.ChestId);
            }
        }

        return openedChestIds.ToArray();
    }

    private static bool CaptureTownTutorialCompleted()
    {
        TownTutorialTrigger[] triggers = FindObjectsByType<TownTutorialTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < triggers.Length; i++)
        {
            if (triggers[i] != null && triggers[i].IsCompleted)
            {
                return true;
            }
        }

        return Instance != null && Instance.IsTownTutorialCompleted;
    }



    private static void RestoreInventory(PlayerInventory inventory, Dictionary<string, ItemData> itemRegistry, string[] inventoryItemIds)
    {
        if (inventoryItemIds == null)
        {
            return;
        }

        for (int i = 0; i < inventoryItemIds.Length; i++)
        {
            ItemData item = FindItem(itemRegistry, inventoryItemIds[i]);
            if (item != null)
            {
                inventory.AddItem(item);
            }
        }
    }

    private static void RestoreEquipment(EquipmentManager equipmentManager, Dictionary<string, ItemData> itemRegistry, EquipmentSaveData equipment)
    {
        if (equipment == null)
        {
            return;
        }

        EquipIfPresent(equipmentManager, FindItem(itemRegistry, equipment.swordItemId));
        EquipIfPresent(equipmentManager, FindItem(itemRegistry, equipment.shieldItemId));
        EquipIfPresent(equipmentManager, FindItem(itemRegistry, equipment.helmetItemId));
        EquipIfPresent(equipmentManager, FindItem(itemRegistry, equipment.chestItemId));
        EquipIfPresent(equipmentManager, FindItem(itemRegistry, equipment.legsItemId));
    }

    private static void EquipIfPresent(EquipmentManager equipmentManager, ItemData item)
    {
        if (item != null)
        {
            equipmentManager.EquipItem(item);
        }
    }

    private static string GetItemId(ItemData item)
    {
        return item != null ? item.itemId : null;
    }

    private static ItemData FindItem(Dictionary<string, ItemData> itemRegistry, string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return null;
        }

        itemRegistry.TryGetValue(itemId, out ItemData item);
        return item;
    }

    private static QuestData FindQuest(Dictionary<string, QuestData> questRegistry, string questId)
    {
        if (string.IsNullOrWhiteSpace(questId))
        {
            return null;
        }

        questRegistry.TryGetValue(questId, out QuestData quest);
        return quest;
    }

    private static IReadOnlyList<QuestData> ResolveQuests(Dictionary<string, QuestData> questRegistry, string[] questIds)
    {
        List<QuestData> quests = new List<QuestData>();
        if (questIds == null)
        {
            return quests;
        }

        for (int i = 0; i < questIds.Length; i++)
        {
            QuestData quest = FindQuest(questRegistry, questIds[i]);
            if (quest != null)
            {
                quests.Add(quest);
            }
        }

        return quests;
    }

    private static void ApplyChestState()
    {
        HashSet<string> openedChestIds = new HashSet<string>();
        ChestInteractable[] chests = FindObjectsByType<ChestInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < chests.Length; i++)
        {
            if (chests[i] != null)
            {
                chests[i].RestoreOpenedState(false);
            }
        }

        if (Instance == null || Instance._saveData == null || Instance._saveData.openedChestIds == null)
        {
            return;
        }

        for (int i = 0; i < Instance._saveData.openedChestIds.Length; i++)
        {
            string chestId = Instance._saveData.openedChestIds[i];
            if (!string.IsNullOrWhiteSpace(chestId))
            {
                openedChestIds.Add(chestId);
            }
        }

        for (int i = 0; i < chests.Length; i++)
        {
            ChestInteractable chest = chests[i];
            if (chest != null)
            {
                chest.RestoreOpenedState(openedChestIds.Contains(chest.ChestId));
            }
        }
    }

    private static void ApplyTutorialState()
    {
        bool townTutorialCompleted = Instance != null && Instance.IsTownTutorialCompleted;
        bool chestTutorialCompleted = Instance != null && Instance.IsChestTutorialCompleted;

        TownTutorialTrigger[] triggers = FindObjectsByType<TownTutorialTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < triggers.Length; i++)
        {
            if (triggers[i] != null)
            {
                triggers[i].RestoreCompletedState(townTutorialCompleted);
            }
        }

    }

    private static void ApplyMerchantState(Dictionary<string, ItemData> itemRegistry)
    {
        MerchantInteractable[] merchants = FindObjectsByType<MerchantInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Dictionary<string, MerchantSaveData> merchantSaveMap = new Dictionary<string, MerchantSaveData>();

        if (Instance != null && Instance._saveData != null && Instance._saveData.merchants != null)
        {
            for (int i = 0; i < Instance._saveData.merchants.Length; i++)
            {
                MerchantSaveData merchantSave = Instance._saveData.merchants[i];
                if (merchantSave != null && !string.IsNullOrWhiteSpace(merchantSave.merchantId))
                {
                    merchantSaveMap[merchantSave.merchantId] = merchantSave;
                }
            }
        }

        for (int i = 0; i < merchants.Length; i++)
        {
            MerchantInteractable merchant = merchants[i];
            if (merchant == null)
            {
                continue;
            }

            if (!merchantSaveMap.TryGetValue(merchant.MerchantId, out MerchantSaveData merchantSave))
            {
                continue;
            }

            List<ItemData> restoredStock = new List<ItemData>();
            if (merchantSave.remainingStockItemIds != null)
            {
                for (int itemIndex = 0; itemIndex < merchantSave.remainingStockItemIds.Length; itemIndex++)
                {
                    ItemData item = FindItem(itemRegistry, merchantSave.remainingStockItemIds[itemIndex]);
                    if (item != null)
                    {
                        restoredStock.Add(item);
                    }
                }
            }

            merchant.RestoreStock(restoredStock);
        }
    }

    private static Dictionary<string, ItemData> BuildItemRegistry(EquipmentManager equipmentManager)
    {
        Dictionary<string, ItemData> itemRegistry = new Dictionary<string, ItemData>();
        RegisterItems(itemRegistry, Resources.FindObjectsOfTypeAll<ItemData>());

        MerchantInteractable[] merchants = FindObjectsByType<MerchantInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < merchants.Length; i++)
        {
            RegisterItems(itemRegistry, merchants[i] != null ? merchants[i].Stock : null);
        }

        ChestInteractable[] chests = FindObjectsByType<ChestInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < chests.Length; i++)
        {
            RegisterItems(itemRegistry, chests[i] != null ? chests[i].RewardItems : null);
        }

        if (equipmentManager != null)
        {
            RegisterItems(itemRegistry, new[]
            {
                equipmentManager.DefaultSword,
                equipmentManager.DefaultShield,
                equipmentManager.DefaultHelmet,
                equipmentManager.DefaultChest,
                equipmentManager.DefaultLegs,
                equipmentManager.EquippedSword,
                equipmentManager.EquippedShield,
                equipmentManager.EquippedHelmet,
                equipmentManager.EquippedChest,
                equipmentManager.EquippedLegs
            });
        }

        return itemRegistry;
    }

    private static Dictionary<string, QuestData> BuildQuestRegistry()
    {
        Dictionary<string, QuestData> questRegistry = new Dictionary<string, QuestData>();
        RegisterQuests(questRegistry, Resources.FindObjectsOfTypeAll<QuestData>());

        NPCQuestGiver[] questGivers = FindObjectsByType<NPCQuestGiver>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < questGivers.Length; i++)
        {
            QuestData quest = questGivers[i] != null ? questGivers[i].QuestData : null;
            if (quest != null && !string.IsNullOrWhiteSpace(quest.QuestId))
            {
                questRegistry[quest.QuestId] = quest;
            }
        }

        return questRegistry;
    }

    private static void RegisterItems(Dictionary<string, ItemData> itemRegistry, IReadOnlyList<ItemData> items)
    {
        if (items == null)
        {
            return;
        }

        for (int i = 0; i < items.Count; i++)
        {
            ItemData item = items[i];
            if (item != null && !string.IsNullOrWhiteSpace(item.itemId))
            {
                itemRegistry[item.itemId] = item;
            }
        }
    }

    private static void RegisterQuests(Dictionary<string, QuestData> questRegistry, IReadOnlyList<QuestData> quests)
    {
        if (quests == null)
        {
            return;
        }

        for (int i = 0; i < quests.Count; i++)
        {
            QuestData quest = quests[i];
            if (quest != null && !string.IsNullOrWhiteSpace(quest.QuestId))
            {
                questRegistry[quest.QuestId] = quest;
            }
        }
    }
}







