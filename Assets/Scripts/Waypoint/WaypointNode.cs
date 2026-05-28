using UnityEngine;

public enum WaypointType
{
    Town,
    Region
}

public class WaypointNode : MonoBehaviour
{
    [SerializeField]
    private string _waypointId;

    [SerializeField]
    private string _displayName;

    [SerializeField]
    private WaypointType _waypointType;

    [SerializeField]
    private Transform _spawnPoint;

    [SerializeField]
    private GameObject _portalVisual;

    public string WaypointId => _waypointId;
    public string DisplayName => _displayName;
    public WaypointType WaypointType => _waypointType;
    public Transform SpawnPoint => _spawnPoint;
    public bool HasPortalVisual => _portalVisual != null;

    private void Awake()
    {
        if (_spawnPoint == null)
        {
            _spawnPoint = transform;
        }
    }

    public void SetPortalActive(bool isActive)
    {
        if (_portalVisual != null)
        {
            _portalVisual.SetActive(isActive);
        }
    }
}
