using UnityEngine;
using Cinemachine;

public class PlayerCameraZoom : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera _vcam;
    [SerializeField] private float _idleFOV = 40f;
    [SerializeField] private float _dashFOV = 50f;
    [SerializeField] private float _zoomSpeed = 5f;

    private PlayerMovement _playerMovement;

    private void Awake()
    {
        if (_vcam == null)
            _vcam = GetComponent<CinemachineVirtualCamera>();
            
        _playerMovement = Object.FindAnyObjectByType<PlayerMovement>();
    }

    private void Update()
    {
        if (_vcam == null || _playerMovement == null) return;

        float targetFOV = _playerMovement.IsDashing ? _dashFOV : _idleFOV;
        
        _vcam.m_Lens.FieldOfView = Mathf.Lerp(_vcam.m_Lens.FieldOfView, targetFOV, Time.deltaTime * _zoomSpeed);
    }
}
