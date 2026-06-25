using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class LegacyLocalSaveImporter : MonoBehaviour
{
    private const string SaveFileName = "savegame.json";
    private const string EncryptedSaveFormat = "ProjectOriginEncryptedSave";
    private const int EncryptedSaveVersion = 1;
    private const int AesKeySizeBytes = 32;
    private const int HmacKeySizeBytes = 32;
    private const string SaveEncryptionSecret = "ProjectOrigin_SaveEncryption_v1_9F4C3A44C1E54F32A7E0D6D97B708A22";

    [Serializable]
#pragma warning disable 0649
    private class LegacyEncryptedSavePayload
    {
        public string format;
        public int version;
        public int iterations;
        public string salt;
        public string iv;
        public string ciphertext;
        public string hmac;
    }
#pragma warning restore 0649

    public static string LegacySavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    public static bool HasLegacyLocalSave()
    {
        return File.Exists(LegacySavePath);
    }

    public static bool TryLoadLegacySave(out SaveData saveData, out string error)
    {
        saveData = null;
        error = null;

        if (!HasLegacyLocalSave())
        {
            error = "기존 로컬 세이브 파일이 없습니다.";
            return false;
        }

        string saveText;
        try
        {
            saveText = File.ReadAllText(LegacySavePath);
        }
        catch (Exception ex)
        {
            error = $"기존 로컬 세이브 파일을 읽지 못했습니다: {ex.Message}";
            return false;
        }

        if (string.IsNullOrWhiteSpace(saveText))
        {
            error = "기존 로컬 세이브 파일이 비어 있습니다.";
            return false;
        }

        string json;
        if (TryDecryptSaveJson(saveText, out json))
        {
            return TryParseSaveData(json, out saveData, out error);
        }

        if (IsEncryptedSavePayload(saveText))
        {
            error = "기존 암호화 세이브 파일을 복호화하지 못했습니다.";
            return false;
        }

        return TryParseSaveData(saveText, out saveData, out error);
    }

    public static string GetImportedPrefsKey(string userId)
    {
        return $"LegacyLocalSaveImported_{userId}";
    }

    private static bool TryParseSaveData(string json, out SaveData saveData, out string error)
    {
        saveData = null;
        error = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            error = "기존 로컬 세이브 JSON이 비어 있습니다.";
            return false;
        }

        try
        {
            saveData = JsonUtility.FromJson<SaveData>(json);
            if (saveData == null)
            {
                error = "기존 로컬 세이브 데이터를 파싱하지 못했습니다.";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            error = $"기존 로컬 세이브 JSON 파싱 실패: {ex.Message}";
            return false;
        }
    }

    private static bool TryDecryptSaveJson(string saveText, out string json)
    {
        json = null;

        LegacyEncryptedSavePayload payload;
        try
        {
            payload = JsonUtility.FromJson<LegacyEncryptedSavePayload>(saveText);
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

            DeriveKeys(salt, payload.iterations, out byte[] encryptionKey, out byte[] hmacKey);
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
            LegacyEncryptedSavePayload payload = JsonUtility.FromJson<LegacyEncryptedSavePayload>(saveText);
            return payload != null && payload.format == EncryptedSaveFormat;
        }
        catch
        {
            return false;
        }
    }

    private static void DeriveKeys(byte[] salt, int iterations, out byte[] encryptionKey, out byte[] hmacKey)
    {
        byte[] password = Encoding.UTF8.GetBytes($"{Application.companyName}|{Application.productName}|{SaveEncryptionSecret}");
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
}


