using UnityEngine;

public class BossRoomTrigger : MonoBehaviour
{
    [SerializeField] private BossMonster boss;
    [SerializeField] private GameObject bossObject;
    [SerializeField] private BossHpBarUi bossHpBarUi;

    private bool triggered;

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
        if (triggered || !IsPlayerCollider(other))
        {
            return;
        }

        if (bossObject != null && !bossObject.activeSelf)
        {
            bossObject.SetActive(true);
        }

        BossMonster targetBoss = boss;
        if (targetBoss == null && bossObject != null)
        {
            targetBoss = bossObject.GetComponent<BossMonster>();
        }

        if (targetBoss == null)
        {
            Debug.LogWarning($"{name} could not find BossMonster to trigger.", this);
            return;
        }

        triggered = true;
        bossHpBarUi?.Show(targetBoss);
        targetBoss.BeginSpawn();
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
