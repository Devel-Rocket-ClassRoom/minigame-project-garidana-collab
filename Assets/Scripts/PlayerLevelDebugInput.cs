using UnityEngine;

public class PlayerLevelDebugInput : MonoBehaviour
{
    [SerializeField] private PlayerStats _playerStats;
    [SerializeField] private KeyCode _levelUpKey = KeyCode.L;

#if UNITY_EDITOR
    private void Update()
    {
        if (_playerStats == null)
        {
            return;
        }

        if (Input.GetKeyDown(_levelUpKey))
        {
            _playerStats.AddLevel(1);
        }
    }
#endif
}