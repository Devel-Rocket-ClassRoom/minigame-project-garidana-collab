using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Text;

public class InteractionUi : MonoBehaviour
{
    [SerializeField]
    private GameObject _root;

    [SerializeField] 
    private TextMeshProUGUI _promptText;

    [SerializeField]
    private GameObject _waypointSelectionRoot;

    [SerializeField]
    private TextMeshProUGUI _waypointSelectionText;

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

        if (_waypointSelectionRoot != null)
        {
            _waypointSelectionRoot.SetActive(false);
        }
    }

    public void ShowWaypointSelection(string prompt, IReadOnlyList<string> destinationNames)
    {
        Show(prompt);

        if (_waypointSelectionRoot == null || _waypointSelectionText == null)
        {
            return;
        }

        _waypointSelectionRoot.SetActive(true);

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < destinationNames.Count; i++)
        {
            builder.Append(i + 1);
            builder.Append(". ");
            builder.Append(destinationNames[i]);

            if (i < destinationNames.Count - 1)
            {
                builder.AppendLine();
            }
        }

        if (builder.Length > 0)
        {
            builder.AppendLine();
        }

        builder.Append("F. Close");
        _waypointSelectionText.text = builder.ToString();
    }

    public void Hide()
    {
        if (_root != null)
        {
            _root.SetActive(false);
        }

        if (_waypointSelectionRoot != null)
        {
            _waypointSelectionRoot.SetActive(false);
        }
    }
}
