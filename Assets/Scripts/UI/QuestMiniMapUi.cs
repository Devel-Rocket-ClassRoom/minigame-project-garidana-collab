using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class QuestMiniMapUi : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private Camera miniMapCamera;
    [SerializeField] private RectTransform markerBounds;
    [SerializeField] private RectTransform questMarker;
    [SerializeField] private RectTransform edgeIndicator;

    [Header("Marker Sprites")]
    [SerializeField] private Image questMarkerImage;
    [SerializeField] private Sprite questMarkerDefaultSprite;
    [SerializeField] private Sprite questMarkerReadySprite;
    [SerializeField] private Image edgeIndicatorImage;
    [SerializeField] private Sprite edgeIndicatorDefaultSprite;
    [SerializeField] private Sprite edgeIndicatorReadySprite;

    [Header("Camera Tracking")]
    [SerializeField] private float cameraHeight = 45f;

    [Header("Markers")]
    [SerializeField] private Vector3 markerWorldOffset = new Vector3(0f, 2f, 0f);
    [SerializeField] private float edgePadding = 18f;

    private NPCQuestGiver _targetNpc;
    private PlayerStats _playerStats;
    private QuestManager _questManager;
    private bool _subscribed;

    private void Awake()
    {
        _playerStats = playerStats != null ? playerStats : FindFirstObjectByType<PlayerStats>();
    }

    private void OnEnable()
    {
        TrySubscribe();
        RefreshTargetNpc();
    }

    private void Start()
    {
        ResolveMissingReferences();
        TrySubscribe();
        RefreshTargetNpc();
    }

    private void LateUpdate()
    {
        if (_playerStats == null)
        {
            _playerStats = playerStats != null ? playerStats : FindFirstObjectByType<PlayerStats>();
        }

        if (_playerStats == null || miniMapCamera == null || markerBounds == null || questMarker == null || edgeIndicator == null)
        {
            return;
        }

        ApplyMarkerSprites();

        UpdateCameraTransform();
        UpdateQuestGuide();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void UpdateCameraTransform()
    {
        Transform playerTransform = _playerStats.transform;
        Vector3 playerPosition = playerTransform.position;

        miniMapCamera.transform.position = new Vector3(playerPosition.x, playerPosition.y + cameraHeight, playerPosition.z);
    }

    private void TrySubscribe()
    {
        if (_subscribed)
        {
            return;
        }

        _questManager = QuestManager.Instance;
        if (_questManager == null)
        {
            return;
        }

        _questManager.QuestAccepted += HandleQuestChanged;
        _questManager.QuestReadyToComplete += HandleQuestChanged;
        _questManager.QuestCompleted += HandleQuestChanged;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed || _questManager == null)
        {
            return;
        }

        _questManager.QuestAccepted -= HandleQuestChanged;
        _questManager.QuestReadyToComplete -= HandleQuestChanged;
        _questManager.QuestCompleted -= HandleQuestChanged;
        _subscribed = false;
    }

    private void HandleQuestChanged(QuestData _)
    {
        RefreshTargetNpc();
    }

    private void RefreshTargetNpc()
    {
        _questManager = QuestManager.Instance;
        NPCQuestGiver[] questGivers = FindObjectsByType<NPCQuestGiver>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        if (_questManager == null || questGivers == null || questGivers.Length == 0)
        {
            _targetNpc = null;
            return;
        }

        QuestData currentQuest = _questManager.CurrentQuest;
        if (currentQuest != null)
        {
            _targetNpc = questGivers.FirstOrDefault(giver => giver != null && giver.QuestData == currentQuest);
            if (_targetNpc != null)
            {
                return;
            }
        }

        _targetNpc = questGivers
            .Where(giver => giver != null && giver.QuestData != null && _questManager.GetQuestState(giver.QuestData) == QuestState.Available)
            .OrderBy(giver => giver.QuestData.Chapter)
            .ThenBy(giver => giver.QuestData.QuestId, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private void UpdateQuestGuide()
    {
        if (questMarker == null || edgeIndicator == null || markerBounds == null)
        {
            return;
        }

        if (_targetNpc == null)
        {
            RefreshTargetNpc();
        }

        if (_targetNpc == null || !_targetNpc.isActiveAndEnabled)
        {
            questMarker.gameObject.SetActive(false);
            edgeIndicator.gameObject.SetActive(false);
            return;
        }

        bool shouldShowEdgeIndicator = ShouldShowEdgeIndicator();

        Vector3 worldPoint = _targetNpc.Transform.position + markerWorldOffset;
        Vector3 viewport = miniMapCamera.WorldToViewportPoint(worldPoint);
        if (viewport.z <= 0f)
        {
            questMarker.gameObject.SetActive(false);
            edgeIndicator.gameObject.SetActive(shouldShowEdgeIndicator);
            return;
        }

        Vector2 rectSize = markerBounds.rect.size;
        Vector2 centeredViewport = new Vector2((viewport.x - 0.5f) * rectSize.x, (viewport.y - 0.5f) * rectSize.y);

        bool isInside = viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
        if (isInside)
        {
            questMarker.gameObject.SetActive(true);
            edgeIndicator.gameObject.SetActive(false);
            questMarker.anchoredPosition = centeredViewport;
            return;
        }

        questMarker.gameObject.SetActive(false);
        edgeIndicator.gameObject.SetActive(shouldShowEdgeIndicator);

        if (!shouldShowEdgeIndicator)
        {
            return;
        }

        Vector2 direction = centeredViewport.sqrMagnitude > 0.001f
            ? centeredViewport.normalized
            : Vector2.up;

        float halfWidth = (rectSize.x * 0.5f) - edgePadding;
        float halfHeight = (rectSize.y * 0.5f) - edgePadding;
        float scale = Mathf.Min(
            Mathf.Abs(direction.x) > 0.001f ? halfWidth / Mathf.Abs(direction.x) : float.MaxValue,
            Mathf.Abs(direction.y) > 0.001f ? halfHeight / Mathf.Abs(direction.y) : float.MaxValue);

        Vector2 clampedPosition = direction * scale;
        edgeIndicator.anchoredPosition = clampedPosition;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        edgeIndicator.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void ResolveMissingReferences()
    {
        if (_playerStats == null && playerStats != null)
        {
            _playerStats = playerStats;
        }

        if (questMarkerImage == null && questMarker != null)
        {
            questMarkerImage = questMarker.GetComponent<Image>();
        }

        if (edgeIndicatorImage == null && edgeIndicator != null)
        {
            edgeIndicatorImage = edgeIndicator.GetComponent<Image>();
        }
    }

    private void ApplyMarkerSprites()
    {
        bool isReadyToComplete = IsTargetQuestReadyToComplete();

        if (questMarkerImage != null)
        {
            Sprite markerSprite = isReadyToComplete && questMarkerReadySprite != null
                ? questMarkerReadySprite
                : questMarkerDefaultSprite;

            if (markerSprite != null)
            {
                questMarkerImage.sprite = markerSprite;
            }
        }

        if (edgeIndicatorImage != null)
        {
            Sprite indicatorSprite = isReadyToComplete && edgeIndicatorReadySprite != null
                ? edgeIndicatorReadySprite
                : edgeIndicatorDefaultSprite;

            if (indicatorSprite != null)
            {
                edgeIndicatorImage.sprite = indicatorSprite;
            }
        }
    }

    private bool IsTargetQuestReadyToComplete()
    {
        if (_questManager == null || _targetNpc == null || _targetNpc.QuestData == null)
        {
            return false;
        }

        return _questManager.GetQuestState(_targetNpc.QuestData) == QuestState.ReadyToComplete;
    }

    private bool ShouldShowEdgeIndicator()
    {
        if (_questManager == null || _targetNpc == null || _targetNpc.QuestData == null)
        {
            return false;
        }

        QuestState state = _questManager.GetQuestState(_targetNpc.QuestData);
        return state == QuestState.Available || state == QuestState.ReadyToComplete;
    }
}
