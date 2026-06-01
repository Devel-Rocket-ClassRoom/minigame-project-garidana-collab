using UnityEngine;
using System.Collections;

public class QuestItemCollectEffect : MonoBehaviour
{
    [SerializeField]   
    private float duration = 0.65f;
    
    [SerializeField]
    private float arcHeight = 1.5f;
    [SerializeField]
    private float scatterRadius = 0.8f;
    [SerializeField]
    private Vector3 targetOffset = new Vector3 (0f, 1f, 0f);

    private ItemData itemData;
    private Transform target;
    private bool initialized;


    public void Initialize (ItemData itemData, Transform target)
    {
        if (itemData == null || target == null)
        {
            Destroy(gameObject);
            return;
        }

        this.itemData = itemData;
        this.target = target;
        initialized = true;

        StartCoroutine(MoveToTargetRoutine());
    }

    private IEnumerator MoveToTargetRoutine()
    {
        Vector3 startPos = transform.position;
        Vector3 randomDir = Random.insideUnitSphere;
        randomDir.y = 0f;

        if (randomDir.sqrMagnitude < 0.01f)
        {
            randomDir = Vector3.forward;
        }

        randomDir.Normalize();

        Vector3 controlPoint = startPos
            + randomDir * Random.Range(0.2f, scatterRadius)
            + Vector3.up * arcHeight;


        float elapsed = 0f;
        float safeDuration = Mathf.Max (0.01f, duration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / safeDuration);
            float smoothT = Mathf.SmoothStep(0f,1f,t);

            Vector3 endPos = target.position + targetOffset;

            Vector3 a = Vector3.Lerp(startPos, controlPoint, smoothT);
            Vector3 b = Vector3.Lerp(controlPoint, endPos, smoothT);
            transform.position = Vector3.Lerp(a, b, smoothT);

            transform.Rotate(Vector3.up, 360f * Time.deltaTime, Space.World);
            FaceCamera();

            yield return null;
        }

        CompleteCollect();
    }

    private void FaceCamera()
    {
        if (Camera.main == null)
        {
            return;
        }

        transform.rotation = Camera.main.transform.rotation;
    }

    private void CompleteCollect()
    {
        if (!initialized || itemData == null)
        {
            Destroy(gameObject);
            return;
        }

        QuestManager.Instance?.ReportItemCollected(itemData.itemId);

        Destroy(gameObject);
    }
}

