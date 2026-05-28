using TMPro;
using UnityEngine;

public class FloatingHudGoldTextEffect : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI text;

    [SerializeField]
    private float duration = 0.8f;

    [SerializeField]
    private float riseDistance = 45f;

    private RectTransform rectTransform;
    private Vector2 startAnchoredPosition;
    private Color startColor;
    private float elapsed;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (text == null)
        {
            text = GetComponentInChildren<TextMeshProUGUI>();
        }
    }

    public void Initialize(string value, Color color)
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        if (text == null)
        {
            text = GetComponentInChildren<TextMeshProUGUI>();
        }

        text.text = value;
        text.color = color;

        startAnchoredPosition = rectTransform.anchoredPosition;
        startColor = color;
        elapsed = 0f;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        float t = Mathf.Clamp01(elapsed / duration);
        float smoothT = Mathf.SmoothStep(0f, 1f, t);

        rectTransform.anchoredPosition =
            startAnchoredPosition + Vector2.up * (riseDistance * smoothT);

        Color color = startColor;
        color.a = 1f - t;
        text.color = color;

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }
}