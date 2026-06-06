using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 플레이어가 구역에 진입하면 해당 구역의 라이팅 프로필로 전환한다.
/// BgmZoneTrigger와 동일한 우선순위 패턴. 구역 경계에 트리거 콜라이더와 함께 배치.
/// </summary>
public class LightingZoneTrigger : MonoBehaviour
{
    private static readonly List<LightingZoneTrigger> ActiveZones = new List<LightingZoneTrigger>();

    [SerializeField]
    private LightingZoneProfile profile;

    [SerializeField]
    [Tooltip("이 구역의 포스트프로세싱 Volume (선택). Global 모드, weight 0으로 배치할 것.")]
    private Volume zoneVolume;

    [SerializeField]
    private float fadeDuration = 2f;

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

        ApplyHighestPriorityZone();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        ActiveZones.Remove(this);
        ApplyHighestPriorityZone();
    }

    private void OnDisable()
    {
        if (ActiveZones.Remove(this))
        {
            ApplyHighestPriorityZone();
        }
    }

    private static void ApplyHighestPriorityZone()
    {
        LightingZoneTrigger bestZone = null;

        for (int i = 0; i < ActiveZones.Count; i++)
        {
            LightingZoneTrigger zone = ActiveZones[i];
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

        LightingZoneController.Instance?.BlendTo(bestZone.profile, bestZone.zoneVolume, bestZone.fadeDuration);
    }
}
