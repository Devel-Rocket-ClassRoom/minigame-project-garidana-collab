using UnityEngine;

public class WaypointInteractable : MonoBehaviour, IInteractable
{
    [SerializeField]
    private WaypointNode _waypointNode;

    [SerializeField]
    private string _lockedPrompt = "Activate Waypoint";

    [SerializeField]
    private string _unlockedPrompt = "Use Waypoint";

    public string InteractionPrompt
    {
        get
        {
            if (_waypointNode == null)
            {
                return "Waypoint";
            }

            return IsUnlocked() ? _unlockedPrompt : _lockedPrompt;
        }
    }

    public Transform Transform => _waypointNode != null ? _waypointNode.transform : transform;

    private void Awake()
    {
        if (_waypointNode == null)
        {
            _waypointNode = GetComponent<WaypointNode>();
        }
    }

    public bool CanInteract(GameObject interactor)
    {
        return WaypointManager.Instance != null && _waypointNode != null;
    }

    public void Interact(GameObject interactor)
    {
        if (WaypointManager.Instance == null || _waypointNode == null)
        {
            return;
        }

        if (!IsUnlocked())
        {
            WaypointManager.Instance.UnlockWaypoint(_waypointNode.WaypointId);
            return;
        }

        WaypointManager.Instance.SetLastActivatedWaypoint(_waypointNode.WaypointId);

        if (WaypointManager.Instance.TryGetTravelDestination(_waypointNode.WaypointId, out WaypointNode destination))
        {
            WaypointManager.Instance.TeleportPlayerTo(destination.WaypointId, interactor);
            return;
        }

        Debug.Log($"이동 가능한 웨이포인트가 없습니다: {_waypointNode.WaypointId}");
    }

    private bool IsUnlocked()
    {
        return WaypointManager.Instance != null
            && WaypointManager.Instance.IsUnlocked(_waypointNode.WaypointId);
    }
}
