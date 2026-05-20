using UnityEngine;
using Cinemachine;

public class PlayerCameraZoom : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera _vcam;
    [SerializeField] private float _idleFOV = 40f;
    [SerializeField] private float _sprintFOV = 50f;
    [SerializeField] private float _zoomSpeed = 5f;

    private PlayerInputReader _inputReader;
    private PlayerMovement _playerMovement;

    private void Awake()
    {
        if (_vcam == null)
            _vcam = GetComponent<CinemachineVirtualCamera>();
            
        _inputReader = Object.FindAnyObjectByType<PlayerInputReader>();
        _playerMovement = Object.FindAnyObjectByType<PlayerMovement>();
    }

    private void Update()
    {
        if (_vcam == null || _playerMovement == null || _inputReader == null) return;

        // Zoom out during dash OR while sprinting
        bool isZooming = _playerMovement.IsDashing || (_inputReader.IsSprinting && _inputReader.MoveInput.magnitude > 0.1f);
        float targetFOV = isZooming ? _sprintFOV : _idleFOV;
        
        _vcam.m_Lens.FieldOfView = Mathf.Lerp(_vcam.m_Lens.FieldOfView, targetFOV, Time.deltaTime * _zoomSpeed);
    }

    private bool IsMoving()
    {
        // Simple check for movement input
        return _inputReader.MoveInput.magnitude > 0.1f;
    }
}