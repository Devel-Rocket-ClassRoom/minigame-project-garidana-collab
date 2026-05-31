using UnityEngine;
using System.Collections;
using System.Drawing;
using UnityEngine.Rendering;


public class EquipmentCollectEffect : MonoBehaviour
{
    [SerializeField]
    private float _duration =  0.65f;

    [SerializeField]
    private float _arcHeigth = 1.5f;

    [SerializeField]
    private float _scatterRadius = 0.8f;

    [SerializeField]
    private Vector3 _targetOffset = new Vector3 (0f, 1f, 0f);

    private ItemData _itemData;
    private Transform _target;
    private PlayerInventory _inventory;
    private bool _initialized;

    public void Initialize (ItemData itemData, Transform target, PlayerInventory inventory)
    {
        if (itemData == null || target == null || inventory == null)
        {
            Destroy(gameObject);
            return;
        }

        _itemData = itemData;
        _target = target;
        _inventory = inventory;
        _initialized = true;

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
            + randomDir * Random.Range(0.2f, _scatterRadius)
            + Vector3.up * _arcHeigth;

        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, _duration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / safeDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            Vector3 endPos = _target.position + _targetOffset;

            Vector3 a = Vector3.Lerp(startPos, controlPoint, smoothT);
            Vector3 b = Vector3.Lerp(controlPoint, endPos, smoothT);
            transform.position = Vector3.Lerp(a,b, smoothT);

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
        if (!_initialized || _itemData == null || _inventory == null)
        {
            Destroy(gameObject);
            return;
        }

        if (_inventory.HasItem(_itemData))
        {
            Debug.Log($"[EquipmentCollect] 이미 보유 중인 장비입니다: {_itemData.displayName}");
            Destroy(gameObject);
            return;
        }

        bool added = _inventory.AddItem(_itemData);
        if (!added)
        {
            Debug.Log($"[EquipmentCollect] 인벤토리에 추가하지 못했습니다: {_itemData.displayName}");
        }

        Destroy(gameObject);
    }
}