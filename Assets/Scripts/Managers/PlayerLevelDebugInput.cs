using UnityEngine;

public class PlayerLevelDebugInput : MonoBehaviour
{
    [SerializeField] private PlayerStats _playerStats;
    [SerializeField] private PlayerMovement _playerMovement;
    [SerializeField] private KeyCode _levelUpKey = KeyCode.L;
    [SerializeField] private KeyCode _forceDeathKey = KeyCode.R;
    [SerializeField] private KeyCode _godModeKey = KeyCode.G;
    [SerializeField] private KeyCode _completeQuestKey = KeyCode.K;
    [SerializeField] private float _godModeMoveSpeed = 10f;
    [SerializeField] private float _godModeDashDistance = 5f;
    [SerializeField] private float _godModeAttackPower = 100f;

#if UNITY_EDITOR
    private GUIStyle _godModeLabelStyle;

    private void Awake()
    {
        if (_playerStats == null)
        {
            _playerStats = FindFirstObjectByType<PlayerStats>();
        }

        if (_playerMovement == null)
        {
            _playerMovement = FindFirstObjectByType<PlayerMovement>();
        }
    }

    private void Update()
    {
        if (_playerStats != null && Input.GetKeyDown(_levelUpKey))
        {
            _playerStats.AddLevel(1);
        }

        if (_playerStats != null && Input.GetKeyDown(_forceDeathKey))
        {
            _playerStats.TriggerDebugDeath();
            Debug.Log("Player death triggered by debug input.");
        }

        if (_playerStats != null && Input.GetKeyDown(_godModeKey))
        {
            bool enabled = !_playerStats.DebugGodMode;
            _playerStats.SetDebugGodMode(enabled);
            _playerStats.SetDebugAttackPowerOverride(enabled, _godModeAttackPower);

            if (_playerMovement != null)
            {
                _playerMovement.SetDebugMovementOverride(enabled, _godModeMoveSpeed, _godModeDashDistance);
            }

            Debug.Log($"God mode {(enabled ? "enabled" : "disabled")}");
        }

        if (Input.GetKeyDown(_completeQuestKey))
        {
            bool readyToComplete = QuestManager.Instance != null
                && QuestManager.Instance.MarkCurrentQuestReadyForDebug();

            Debug.Log(readyToComplete
                ? "Current quest objectives completed by debug input. Talk to the NPC to complete the quest."
                : "No current quest objectives to complete.");
        }
    }

    private void OnGUI()
    {
        if (_playerStats == null || !_playerStats.DebugGodMode)
        {
            return;
        }

        if (_godModeLabelStyle == null)
        {
            _godModeLabelStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
            _godModeLabelStyle.normal.textColor = Color.yellow;
        }

        GUI.Box(new Rect(10f, 10f, 130f, 34f), "GOD MODE", _godModeLabelStyle);
    }
#endif
}
