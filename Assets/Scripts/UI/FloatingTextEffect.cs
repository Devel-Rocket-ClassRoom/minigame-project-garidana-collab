using TMPro;
using UnityEngine;

public class FloatingTextEffect : MonoBehaviour
{
    [SerializeField]
    private TextMeshPro textMesh;

    [SerializeField]
    private float duration = 0.8f;

    [SerializeField]
    private float riseDistance = 0.8f;

    private float elapsed;
    private Vector3 startPosition;
    private Color startColor;

    public void Initialize(string text, Color color)
    {
        if (textMesh == null)
        {
            textMesh = GetComponentInChildren<TextMeshPro>();
        }

        textMesh.text = text;
        textMesh.color = color;

        startPosition = transform.position;
        startColor = color;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        float t = Mathf.Clamp01(elapsed / duration);
        transform.position = startPosition + Vector3.up * (riseDistance * t);

        Color color = startColor;
        color.a = 1f - t;
        textMesh.color = color;

        FaceCamera();

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }

    private void FaceCamera()
    {
        if (Camera.main == null)
        {
            return;
        }

        transform.rotation = Camera.main.transform.rotation;
    }
}