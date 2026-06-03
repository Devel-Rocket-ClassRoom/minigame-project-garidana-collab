using UnityEngine;
using System.Collections;

public class BossRoomTrigger : MonoBehaviour
{
    [Header("Boss Spawn")]
    [SerializeField] private BossMonster bossPrefab;
    [SerializeField] private Transform bossSpawnPoint;
    [SerializeField] private BossHpBarUi bossHpBarUi;

    [Header("Boss Room Door")]
    [SerializeField] private Transform entranceDoor;
    [SerializeField] private GameObject entranceBlocker;
    [SerializeField] private float doorRaisedYOffset = 4f;
    [SerializeField] private float doorMoveDuration = 1f;

    [Header("Quest NPC")]
    [SerializeField] private Transform questNpc;
    [SerializeField] private Transform questNpcSpawnPoint;
    [SerializeField] private bool activateQuestNpcOnBossDeath = true;

    private bool triggered;
    private bool encounterCleared;
    private BossMonster activeBoss;
    private Coroutine doorMoveCoroutine;
    private Vector3 doorLoweredLocalPosition;
    private Vector3 doorRaisedLocalPosition;
    private PlayerStats playerStats;

    private void Awake()
    {
        playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats != null)
        {
            playerStats.Died += OnPlayerDied;
        }

        SetEntranceBlockerActive(false);

        if (entranceDoor != null)
        {
            doorLoweredLocalPosition = entranceDoor.localPosition;
            doorRaisedLocalPosition = doorLoweredLocalPosition + Vector3.up * doorRaisedYOffset;
        }
    }

    private void Reset()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayerCollider(other) || encounterCleared)
        {
            return;
        }

        BossMonster targetBoss = activeBoss;
        if (targetBoss == null)
        {
            targetBoss = SpawnBossInstance();
        }

        if (targetBoss == null)
        {
            Debug.LogWarning($"{name} could not find BossMonster to trigger.", this);
            return;
        }

        if (triggered)
        {
            if (!targetBoss.IsDead)
            {
                SetEntranceBlockerActive(true);
                StartDoorAnimation(doorRaisedLocalPosition);
                bossHpBarUi?.Show(targetBoss);
            }

            return;
        }

        triggered = true;
        activeBoss = targetBoss;
        activeBoss.Died -= OnBossDied;
        activeBoss.Died += OnBossDied;

        SetEntranceBlockerActive(true);
        bossHpBarUi?.Show(targetBoss);
        StartDoorAnimation(doorRaisedLocalPosition);
        targetBoss.BeginSpawn();
    }

    private void OnDestroy()
    {
        if (activeBoss != null)
        {
            activeBoss.Died -= OnBossDied;
        }

        if (playerStats != null)
        {
            playerStats.Died -= OnPlayerDied;
        }
    }

    private void OnBossDied()
    {
        encounterCleared = true;
        bossHpBarUi?.Hide();
        SetEntranceBlockerActive(false);
        StartDoorAnimation(doorLoweredLocalPosition);
        MoveQuestNpcToSpawnPoint();

        if (activeBoss != null)
        {
            activeBoss.Died -= OnBossDied;
            activeBoss = null;
        }
    }

    private void OnPlayerDied()
    {
        if (activeBoss == null || activeBoss.IsDead)
        {
            return;
        }

        ResetEncounter();
    }

    private void StartDoorAnimation(Vector3 targetLocalPosition)
    {
        if (entranceDoor == null)
        {
            return;
        }

        if (doorMoveCoroutine != null)
        {
            StopCoroutine(doorMoveCoroutine);
        }

        doorMoveCoroutine = StartCoroutine(AnimateDoor(targetLocalPosition));
    }

    private IEnumerator AnimateDoor(Vector3 targetLocalPosition)
    {
        Vector3 startLocalPosition = entranceDoor.localPosition;
        float duration = Mathf.Max(0.01f, doorMoveDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            entranceDoor.localPosition = Vector3.Lerp(startLocalPosition, targetLocalPosition, t);
            yield return null;
        }

        entranceDoor.localPosition = targetLocalPosition;
        doorMoveCoroutine = null;
    }

    private void SetEntranceBlockerActive(bool isActive)
    {
        if (entranceBlocker != null)
        {
            entranceBlocker.SetActive(isActive);
        }
    }

    private void MoveQuestNpcToSpawnPoint()
    {
        if (questNpc == null || questNpcSpawnPoint == null)
        {
            return;
        }

        if (activateQuestNpcOnBossDeath && !questNpc.gameObject.activeSelf)
        {
            questNpc.gameObject.SetActive(true);
        }

        questNpc.SetPositionAndRotation(questNpcSpawnPoint.position, questNpcSpawnPoint.rotation);

        Rigidbody npcRigidbody = questNpc.GetComponent<Rigidbody>();
        if (npcRigidbody != null)
        {
            npcRigidbody.linearVelocity = Vector3.zero;
            npcRigidbody.angularVelocity = Vector3.zero;
            npcRigidbody.position = questNpcSpawnPoint.position;
            npcRigidbody.rotation = questNpcSpawnPoint.rotation;
        }
    }

    private BossMonster SpawnBossInstance()
    {
        if (bossPrefab == null)
        {
            return null;
        }

        Vector3 spawnPosition = bossSpawnPoint != null ? bossSpawnPoint.position : bossPrefab.transform.position;
        Quaternion spawnRotation = bossSpawnPoint != null ? bossSpawnPoint.rotation : bossPrefab.transform.rotation;

        activeBoss = Instantiate(bossPrefab, spawnPosition, spawnRotation);
        return activeBoss;
    }

    private void ResetEncounter()
    {
        bossHpBarUi?.Hide();
        SetEntranceBlockerActive(false);
        StartDoorAnimation(doorLoweredLocalPosition);

        if (activeBoss != null)
        {
            activeBoss.Died -= OnBossDied;
            Destroy(activeBoss.gameObject);
            activeBoss = null;
        }

        triggered = false;
    }

    private static bool IsPlayerCollider(Collider other)
    {
        if (other == null)
        {
            return false;
        }

        if (other.CompareTag("Player"))
        {
            return true;
        }

        if (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Player"))
        {
            return true;
        }

        return other.GetComponentInParent<PlayerStats>() != null;
    }
}
