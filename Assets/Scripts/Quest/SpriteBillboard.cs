using UnityEngine;

public class SpriteBillboard : MonoBehaviour
{
    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (mainCamera == null) return;

        Vector3 direction = transform.position - mainCamera.transform.position;
        transform.rotation = Quaternion.LookRotation(direction);
    }
}
