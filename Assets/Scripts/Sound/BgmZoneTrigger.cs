using UnityEngine;

public class BgmZoneTrigger : MonoBehaviour
{
    [SerializeField]
    private SoundManager.BGMType bgmType;

    [SerializeField]
    private float fadeDuration = 1f;

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

        SoundManager.Instance?.PlayBGM(bgmType, fadeDuration);
    }
}
