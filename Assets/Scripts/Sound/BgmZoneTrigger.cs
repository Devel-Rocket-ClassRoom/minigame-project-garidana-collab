using System.Collections.Generic;
using UnityEngine;

public class BgmZoneTrigger : MonoBehaviour
{
    private static readonly List<BgmZoneTrigger> ActiveZones = new List<BgmZoneTrigger>();

    [SerializeField]
    private SoundManager.BGMType bgmType;

    [SerializeField]
    private float fadeDuration = 1f;

    [SerializeField]
    private int priority;

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
        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (!ActiveZones.Contains(this))
        {
            ActiveZones.Add(this);
        }

        PlayHighestPriorityZone();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        ActiveZones.Remove(this);
        PlayHighestPriorityZone();
    }

    private void OnDisable()
    {
        if (ActiveZones.Remove(this))
        {
            PlayHighestPriorityZone();
        }
    }

    private static void PlayHighestPriorityZone()
    {
        BgmZoneTrigger bestZone = null;

        for (int i = 0; i < ActiveZones.Count; i++)
        {
            BgmZoneTrigger zone = ActiveZones[i];
            if (zone == null || !zone.isActiveAndEnabled)
            {
                continue;
            }

            if (bestZone == null || zone.priority > bestZone.priority)
            {
                bestZone = zone;
            }
        }

        if (bestZone == null)
        {
            return;
        }

        SoundManager.Instance?.PlayBGM(bestZone.bgmType, bestZone.fadeDuration);
    }
}
