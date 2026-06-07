using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
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
public class EncryptedSavePayload
{
    public string format;
    public int version;
    public int iterations;
    public string salt;
    public string iv;
    public string ciphertext;
    public string hmac;
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
    private const string SaveFileName = "savegame.json";
    private const string EncryptedSaveFormat = "ProjectOriginEncryptedSave";
    private const int EncryptedSaveVersion = 1;
    private const int KeyDerivationIterations = 120000;
    private const int SaltSizeBytes = 16;
    private const int AesKeySizeBytes = 32;
    private const int HmacKeySizeBytes = 32;
    private const string SaveEncryptionSecret = "ProjectOrigin_SaveEncryption_v1_9F4C3A44C1E54F32A7E0D6D97B708A22";

    // PBKDF2 키 유도(12만회 반복, 수백 ms)는 비용이 크므로 1회만 수행하고 캐싱한다.
    // 비밀번호가 고정값이라 솔트 재사용은 안전하며, AES-CBC 보안은 매 저장마다 새로 생성하는 IV가 보장한다.
    private static readonly object KeyCacheLock = new object();
    private static byte[] _passwordBytes;
    private static byte[] _cachedSalt;
    private static int _cachedIterations;
    private static byte[] _cachedEncryptionKey;
    private static byte[] _cachedHmacKey;
#if UNITY_EDITOR
    private static readonly bool LoadSaveInEditor = true;
#endif

    public static SaveManager Instance { get; private set; }
    public bool IsApplyingSave => _isApplyingSave;

    private SaveData _saveData;
    private string _savePath;
    private bool _isApplyingSave;

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
        _savePath = Path.Combine(Application.persistentDataPath, SaveFileName);
        CachePasswordBytes();
        LoadFromDisk();
        WarmupKeysInBackground();
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
        if (_isApplyingSave)
        {
            return;
        }

        SaveData captured = CaptureCurrentState();
        if (captured == null)
        {
            return;
        }

        _saveData = captured;
        string directory = Path.GetDirectoryName(_savePath);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonUtility.ToJson(_saveData, true);
        File.WriteAllText(_savePath, EncryptSaveJson(json));
        Debug.Log($"[Save] 저장 완료: {_savePath}");
    }

    public bool HasSaveData()
    {
        return !string.IsNullOrWhiteSpace(_savePath) && File.Exists(_savePath);
    }

    public bool IsTownTutorialCompleted => _saveData != null
        && _saveData.tutorial != null
        && _saveData.tutorial.townTutorialCompleted;

    public bool IsChestTutorialCompleted => _saveData != null
        && ((_saveData.tutorial != null && _saveData.tutorial.chestTutorialCompleted)
            || (_saveData.openedChestIds != null && _saveData.openedChestIds.Length > 0));

    public void DeleteSaveFile()
    {
        if (!HasSaveData())
        {
            _saveData = null;
            return;
        }

        File.Delete(_savePath);
        _saveData = null;
        Debug.Log($"[Save] 세이브 파일 삭제: {_savePath}");
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
#if UNITY_EDITOR
        if (!LoadSaveInEditor)
        {
            Debug.Log("[Save] 에디터 설정으로 세이브 로드를 건너뜁니다.");
            return;
        }
#endif

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

    private void LoadFromDisk()
    {
        if (!File.Exists(_savePath))
        {
            _saveData = null;
            return;
        }

        string saveText = File.ReadAllText(_savePath);
        if (string.IsNullOrWhiteSpace(saveText))
        {
            _saveData = null;
            return;
        }

        string json;
        if (TryDecryptSaveJson(saveText, out json))
        {
            _saveData = string.IsNullOrWhiteSpace(json) ? null : JsonUtility.FromJson<SaveData>(json);
            return;
        }

        if (IsEncryptedSavePayload(saveText))
        {
            _saveData = null;
            Debug.LogWarning("[Save] 암호화된 세이브 파일을 복호화하지 못했습니다. 파일이 손상되었거나 변조되었을 수 있습니다.");
            return;
        }

        _saveData = JsonUtility.FromJson<SaveData>(saveText);
        Debug.Log("[Save] 기존 평문 세이브 파일을 로드했습니다. 다음 저장 시 암호화 형식으로 마이그레이션됩니다.");
    }

    private static string EncryptSaveJson(string json)
    {
        GetOrCreateCachedKeys(out byte[] salt, out int keyIterations, out byte[] encryptionKey, out byte[] hmacKey);
        byte[] iv;
        byte[] cipherText;

        using (Aes aes = Aes.Create())
        {
            aes.KeySize = AesKeySizeBytes * 8;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = encryptionKey;
            aes.GenerateIV();
            iv = aes.IV;

            using (ICryptoTransform encryptor = aes.CreateEncryptor())
            {
                byte[] plainText = Encoding.UTF8.GetBytes(json);
                cipherText = encryptor.TransformFinalBlock(plainText, 0, plainText.Length);
            }
        }

        byte[] authenticatedData = BuildAuthenticatedData(EncryptedSaveVersion, keyIterations, salt, iv, cipherText);
        byte[] hmac = ComputeHmac(hmacKey, authenticatedData);

        EncryptedSavePayload payload = new EncryptedSavePayload
        {
            format = EncryptedSaveFormat,
            version = EncryptedSaveVersion,
            iterations = keyIterations,
            salt = Convert.ToBase64String(salt),
            iv = Convert.ToBase64String(iv),
            ciphertext = Convert.ToBase64String(cipherText),
            hmac = Convert.ToBase64String(hmac)
        };

        return JsonUtility.ToJson(payload);
    }

    private static bool TryDecryptSaveJson(string saveText, out string json)
    {
        json = null;

        EncryptedSavePayload payload;
        try
        {
            payload = JsonUtility.FromJson<EncryptedSavePayload>(saveText);
        }
        catch
        {
            return false;
        }

        if (payload == null || payload.format != EncryptedSaveFormat)
        {
            return false;
        }

        if (payload.version != EncryptedSaveVersion || payload.iterations <= 0)
        {
            return false;
        }

        try
        {
            byte[] salt = Convert.FromBase64String(payload.salt);
            byte[] iv = Convert.FromBase64String(payload.iv);
            byte[] cipherText = Convert.FromBase64String(payload.ciphertext);
            byte[] expectedHmac = Convert.FromBase64String(payload.hmac);

            GetKeysForSalt(salt, payload.iterations, out byte[] encryptionKey, out byte[] hmacKey);
            byte[] authenticatedData = BuildAuthenticatedData(payload.version, payload.iterations, salt, iv, cipherText);
            byte[] actualHmac = ComputeHmac(hmacKey, authenticatedData);
            if (!FixedTimeEquals(expectedHmac, actualHmac))
            {
                return false;
            }

            using (Aes aes = Aes.Create())
            {
                aes.KeySize = AesKeySizeBytes * 8;
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = encryptionKey;
                aes.IV = iv;

                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    byte[] plainText = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
                    json = Encoding.UTF8.GetString(plainText);
                    return true;
                }
            }
        }
        catch
        {
            json = null;
            return false;
        }
    }

    private static bool IsEncryptedSavePayload(string saveText)
    {
        try
        {
            EncryptedSavePayload payload = JsonUtility.FromJson<EncryptedSavePayload>(saveText);
            return payload != null && payload.format == EncryptedSaveFormat;
        }
        catch
        {
            return false;
        }
    }

    private static byte[] GenerateRandomBytes(int length)
    {
        byte[] bytes = new byte[length];
        using (RandomNumberGenerator generator = RandomNumberGenerator.Create())
        {
            generator.GetBytes(bytes);
        }

        return bytes;
    }

    /// <summary>
    /// Application.* API는 메인 스레드 전용이므로 비밀번호 바이트를 미리 캡처해 둔다.
    /// Awake에서 호출되어 이후 백그라운드 스레드에서도 키 유도가 가능하다.
    /// </summary>
    private static void CachePasswordBytes()
    {
        if (_passwordBytes == null)
        {
            _passwordBytes = Encoding.UTF8.GetBytes($"{Application.companyName}|{Application.productName}|{SaveEncryptionSecret}");
        }
    }

    /// <summary>
    /// 세이브 파일이 없어 로드 시점에 키가 캐싱되지 않은 경우,
    /// 첫 저장에서 렉이 걸리지 않도록 백그라운드에서 미리 키를 유도한다.
    /// </summary>
    private static void WarmupKeysInBackground()
    {
        lock (KeyCacheLock)
        {
            if (_cachedEncryptionKey != null)
            {
                return;
            }
        }

        System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                GetOrCreateCachedKeys(out _, out _, out _, out _);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] 암호화 키 사전 준비 실패: {e.Message}");
            }
        });
    }

    /// <summary>
    /// 캐싱된 키를 반환하고, 없으면 새 솔트로 1회 유도 후 캐싱한다. (저장 경로에서 사용)
    /// </summary>
    private static void GetOrCreateCachedKeys(out byte[] salt, out int iterations, out byte[] encryptionKey, out byte[] hmacKey)
    {
        lock (KeyCacheLock)
        {
            if (_cachedEncryptionKey == null)
            {
                byte[] newSalt = GenerateRandomBytes(SaltSizeBytes);
                DeriveKeys(newSalt, KeyDerivationIterations, out byte[] newEncryptionKey, out byte[] newHmacKey);
                _cachedSalt = newSalt;
                _cachedIterations = KeyDerivationIterations;
                _cachedEncryptionKey = newEncryptionKey;
                _cachedHmacKey = newHmacKey;
            }

            salt = _cachedSalt;
            iterations = _cachedIterations;
            encryptionKey = _cachedEncryptionKey;
            hmacKey = _cachedHmacKey;
        }
    }

    /// <summary>
    /// 주어진 솔트에 대한 키를 반환한다. 캐시와 일치하면 재사용하고,
    /// 아니면 유도 후 캐시를 갱신해 이후 저장에서 재유도를 피한다. (로드 경로에서 사용)
    /// </summary>
    private static void GetKeysForSalt(byte[] salt, int iterations, out byte[] encryptionKey, out byte[] hmacKey)
    {
        lock (KeyCacheLock)
        {
            if (_cachedEncryptionKey != null
                && _cachedIterations == iterations
                && _cachedSalt != null
                && FixedTimeEquals(_cachedSalt, salt))
            {
                encryptionKey = _cachedEncryptionKey;
                hmacKey = _cachedHmacKey;
                return;
            }

            DeriveKeys(salt, iterations, out encryptionKey, out hmacKey);
            _cachedSalt = (byte[])salt.Clone();
            _cachedIterations = iterations;
            _cachedEncryptionKey = encryptionKey;
            _cachedHmacKey = hmacKey;
        }
    }

    private static void DeriveKeys(byte[] salt, int iterations, out byte[] encryptionKey, out byte[] hmacKey)
    {
        byte[] password = _passwordBytes;
        if (password == null)
        {
            CachePasswordBytes();
            password = _passwordBytes;
        }

        using (Rfc2898DeriveBytes deriveBytes = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
        {
            encryptionKey = deriveBytes.GetBytes(AesKeySizeBytes);
            hmacKey = deriveBytes.GetBytes(HmacKeySizeBytes);
        }
    }

    private static byte[] ComputeHmac(byte[] hmacKey, byte[] authenticatedData)
    {
        using (HMACSHA256 hmac = new HMACSHA256(hmacKey))
        {
            return hmac.ComputeHash(authenticatedData);
        }
    }

    private static byte[] BuildAuthenticatedData(int version, int iterations, byte[] salt, byte[] iv, byte[] cipherText)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            writer.Write(EncryptedSaveFormat);
            writer.Write(version);
            writer.Write(iterations);
            WriteBytesWithLength(writer, salt);
            WriteBytesWithLength(writer, iv);
            WriteBytesWithLength(writer, cipherText);
            writer.Flush();
            return stream.ToArray();
        }
    }

    private static void WriteBytesWithLength(BinaryWriter writer, byte[] bytes)
    {
        writer.Write(bytes != null ? bytes.Length : 0);
        if (bytes != null && bytes.Length > 0)
        {
            writer.Write(bytes);
        }
    }

    private static bool FixedTimeEquals(byte[] left, byte[] right)
    {
        if (left == null || right == null || left.Length != right.Length)
        {
            return false;
        }

        int difference = 0;
        for (int i = 0; i < left.Length; i++)
        {
            difference |= left[i] ^ right[i];
        }

        return difference == 0;
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
