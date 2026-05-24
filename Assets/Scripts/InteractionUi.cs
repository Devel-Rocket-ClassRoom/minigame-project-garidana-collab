using UnityEngine;
using TMPro;

public class InteractionUi : MonoBehaviour
{
    [SerializeField]
    private GameObject _root;

    [SerializeField] 
    private TextMeshProUGUI _promptText;

    private void Awake()
    {
        Hide();
    }

    public void Show (string prompt)
    {
        if (_root != null)
        {
            _root.SetActive(true);
        }
        if (_promptText != null)
        {
            _promptText.text = $"[F] {prompt}";
        }
    }

    public void Hide()
    {
        if (_root != null)
        {
            _root.SetActive(false);
        }
    }
}
