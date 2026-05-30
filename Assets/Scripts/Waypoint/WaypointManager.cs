using System.Collections.Generic;
using UnityEngine;

public class WaypointManager : MonoBehaviour
{
    public static WaypointManager Instance { get; private set; }

    [SerializeField]
    private Transform _defaultRespawnPoint;

    [SerializeField]
    private List<WaypointNode> _waypoints = new();

    private readonly HashSet<string> _unlockedWaypointIds = new();
    private readonly Dictionary<string, WaypointNode> _waypointMap = new();

    private string _lastActivatedWaypointId;

    public string LastActivatedWaypointId => _lastActivatedWaypointId;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        BuildWaypointMap();
        SyncWaypointVisuals();
    }

    private void BuildWaypointMap()
    {
        _waypointMap.Clear();

        foreach (WaypointNode waypoint in _waypoints)
        {
            if (waypoint == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(waypoint.WaypointId))
            {
                Debug.LogWarning($"Waypoint ID가 비어 있습니다. Object: {waypoint.name}", waypoint);
                continue;
            }

            if (_waypointMap.ContainsKey(waypoint.WaypointId))
            {
                Debug.LogWarning($"중복된 Waypoint ID가 있습니다: {waypoint.WaypointId}", waypoint);
                continue;
            }

            _waypointMap.Add(waypoint.WaypointId, waypoint);
        }
    }

    public bool IsUnlocked(string waypointId)
    {
        return !string.IsNullOrWhiteSpace(waypointId) && _unlockedWaypointIds.Contains(waypointId);
    }

    public bool UnlockWaypoint(string waypointId)
    {
        if (!_waypointMap.TryGetValue(waypointId, out WaypointNode waypoint))
        {
            Debug.LogWarning($"등록되지 않은 Waypoint unlock 시도: {waypointId}");
            return false;
        }

        bool added = _unlockedWaypointIds.Add(waypointId);

        if (added)
        {
            waypoint.SetPortalActive(true);
            Debug.Log($"웨이포인트 해금: {waypointId}");
        }

        SetLastActivatedWaypoint(waypointId);
        return added;
    }

    public void SetLastActivatedWaypoint(string waypointId)
    {
        if (!_waypointMap.ContainsKey(waypointId))
        {
            Debug.LogWarning($"등록되지 않은 Waypoint last activated 설정 시도: {waypointId}");
            return;
        }

        _lastActivatedWaypointId = waypointId;
    }

    public WaypointNode GetLastActivatedWaypoint()
    {
        if (string.IsNullOrWhiteSpace(_lastActivatedWaypointId))
        {
            return null;
        }

        _waypointMap.TryGetValue(_lastActivatedWaypointId, out WaypointNode waypoint);
        return waypoint;
    }

    public Transform GetLastActivatedSpawnPoint()
    {
        WaypointNode waypoint = GetLastActivatedWaypoint();
        return waypoint != null ? waypoint.SpawnPoint : null;
    }

    public Transform GetRespawnPoint()
    {
        Transform lastActivatedSpawnPoint = GetLastActivatedSpawnPoint();
        if (lastActivatedSpawnPoint != null)
        {
            return lastActivatedSpawnPoint;
        }

        return _defaultRespawnPoint;
    }

    public bool TryGetWaypoint(string waypointId, out WaypointNode waypoint)
    {
        return _waypointMap.TryGetValue(waypointId, out waypoint);
    }

    public bool TryGetTravelDestination(string fromWaypointId, out WaypointNode destination)
    {
        destination = null;

        if (!_waypointMap.TryGetValue(fromWaypointId, out WaypointNode fromWaypoint))
        {
            return false;
        }

        WaypointType targetType = fromWaypoint.WaypointType == WaypointType.Town
            ? WaypointType.Region
            : WaypointType.Town;

        foreach (WaypointNode waypoint in _waypoints)
        {
            if (waypoint == null || waypoint == fromWaypoint)
            {
                continue;
            }

            if (waypoint.WaypointType != targetType)
            {
                continue;
            }

            if (!IsUnlocked(waypoint.WaypointId))
            {
                continue;
            }

            destination = waypoint;
            return true;
        }

        return false;
    }

    public List<WaypointNode> GetTravelDestinations(string fromWaypointId)
    {
        List<WaypointNode> destinations = new();

        if (!_waypointMap.TryGetValue(fromWaypointId, out WaypointNode fromWaypoint))
        {
            return destinations;
        }

        WaypointType targetType = fromWaypoint.WaypointType == WaypointType.Town
            ? WaypointType.Region
            : WaypointType.Town;

        foreach (WaypointNode waypoint in _waypoints)
        {
            if (waypoint == null || waypoint == fromWaypoint)
            {
                continue;
            }

            if (waypoint.WaypointType != targetType)
            {
                continue;
            }

            if (!IsUnlocked(waypoint.WaypointId))
            {
                continue;
            }

            destinations.Add(waypoint);
        }

        return destinations;
    }

    public bool TeleportPlayerTo(string destinationWaypointId, GameObject interactor)
    {
        if (interactor == null)
        {
            return false;
        }

        if (!_waypointMap.TryGetValue(destinationWaypointId, out WaypointNode destination))
        {
            Debug.LogWarning($"등록되지 않은 Waypoint teleport 시도: {destinationWaypointId}");
            return false;
        }

        if (!IsUnlocked(destinationWaypointId))
        {
            Debug.LogWarning($"잠긴 Waypoint로 teleport 시도: {destinationWaypointId}");
            return false;
        }

        Transform spawnPoint = destination.SpawnPoint;
        if (spawnPoint == null)
        {
            Debug.LogWarning($"SpawnPoint가 없는 Waypoint입니다: {destinationWaypointId}", destination);
            return false;
        }

        PlayerStats playerStats = interactor.GetComponent<PlayerStats>();
        if (playerStats == null)
        {
            Debug.LogWarning("텔레포트 대상에 PlayerStats가 없습니다.", interactor);
            return false;
        }

        playerStats.TeleportTo(spawnPoint.position);
        SetLastActivatedWaypoint(destinationWaypointId);
        Debug.Log($"웨이포인트 텔레포트: {destinationWaypointId}");
        return true;
    }

    private void SyncWaypointVisuals()
    {
        foreach (WaypointNode waypoint in _waypointMap.Values)
        {
            waypoint.SetPortalActive(IsUnlocked(waypoint.WaypointId));
        }
    }
}
