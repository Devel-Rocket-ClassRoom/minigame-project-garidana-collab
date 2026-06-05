using UnityEngine;
using Cinemachine;

public class PlayerCameraZoom : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera _vcam;
    [SerializeField] private float _idleFOV = 40f;
    [SerializeField] private float _dashFOV = 50f;
    [SerializeField] private float _zoomSpeed = 5f;
    [SerializeField] private float _minCameraDistance = 4f;
    [SerializeField] private float _maxCameraDistance = 10f;
    [SerializeField] private float _scrollZoomStep = 0.01f;
    [SerializeField] private float _distanceZoomSpeed = 8f;

    private PlayerMovement _playerMovement;
    private PlayerInputReader _inputReader;
    private CinemachineFramingTransposer _framingTransposer;
    private float _targetCameraDistance;

    private void Awake()
    {
        if (_vcam == null)
            _vcam = GetComponent<CinemachineVirtualCamera>();
            
        _playerMovement = Object.FindAnyObjectByType<PlayerMovement>();
        _inputReader = Object.FindAnyObjectByType<PlayerInputReader>();

        if (_vcam != null)
        {
            _framingTransposer = _vcam.GetCinemachineComponent<CinemachineFramingTransposer>();
        }

        if (_framingTransposer != null)
        {
            _targetCameraDistance = Mathf.Clamp(_framingTransposer.m_CameraDistance, _minCameraDistance, _maxCameraDistance);
        }
    }

    private void Update()
    {
        if (_vcam == null || _playerMovement == null) return;

        UpdateCameraDistance();

        float targetFOV = _playerMovement.IsDashing ? _dashFOV : _idleFOV;
        
        _vcam.m_Lens.FieldOfView = Mathf.Lerp(_vcam.m_Lens.FieldOfView, targetFOV, Time.deltaTime * _zoomSpeed);
    }

    private void UpdateCameraDistance()
    {
        if (_framingTransposer == null || _inputReader == null)
        {
            return;
        }

        float scrollInput = _inputReader.CameraZoomInput;
        if (!Mathf.Approximately(scrollInput, 0f))
        {
            _targetCameraDistance = Mathf.Clamp(
                _targetCameraDistance - scrollInput * _scrollZoomStep,
                _minCameraDistance,
                _maxCameraDistance);

            _inputReader.UseCameraZoomInput();
        }

        _framingTransposer.m_CameraDistance = Mathf.Lerp(
            _framingTransposer.m_CameraDistance,
            _targetCameraDistance,
            Time.deltaTime * _distanceZoomSpeed);
    }
}
