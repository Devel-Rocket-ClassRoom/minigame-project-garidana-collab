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
    [SerializeField] private float _angleChangeStartDistance = 7f;
    [SerializeField] private float _closeCameraPitch = 15f;
    [SerializeField] private float _angleZoomSpeed = 8f;
    [SerializeField] private Vector3 _closeTrackedObjectOffset = new Vector3(0f, 1.2f, 0f);

    private PlayerMovement _playerMovement;
    private PlayerInputReader _inputReader;
    private CinemachineFramingTransposer _framingTransposer;
    private float _targetCameraDistance;
    private Vector3 _defaultLocalEulerAngles;
    private Vector3 _defaultTrackedObjectOffset;

    private void Awake()
    {
        if (_vcam == null)
            _vcam = GetComponent<CinemachineVirtualCamera>();
            
        _playerMovement = Object.FindAnyObjectByType<PlayerMovement>();
        _inputReader = Object.FindAnyObjectByType<PlayerInputReader>();

        if (_vcam != null)
        {
            _framingTransposer = _vcam.GetCinemachineComponent<CinemachineFramingTransposer>();
            _defaultLocalEulerAngles = _vcam.transform.localEulerAngles;
        }

        if (_framingTransposer != null)
        {
            _targetCameraDistance = Mathf.Clamp(_framingTransposer.m_CameraDistance, _minCameraDistance, _maxCameraDistance);
            _defaultTrackedObjectOffset = _framingTransposer.m_TrackedObjectOffset;
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

        UpdateCameraAngle();
    }

    private void UpdateCameraAngle()
    {
        float zoomAngleRatio = Mathf.InverseLerp(
            _angleChangeStartDistance,
            _minCameraDistance,
            _framingTransposer.m_CameraDistance);

        Vector3 targetEulerAngles = _defaultLocalEulerAngles;
        targetEulerAngles.x = Mathf.Lerp(_defaultLocalEulerAngles.x, _closeCameraPitch, zoomAngleRatio);

        _vcam.transform.localRotation = Quaternion.Lerp(
            _vcam.transform.localRotation,
            Quaternion.Euler(targetEulerAngles),
            Time.deltaTime * _angleZoomSpeed);

        _framingTransposer.m_TrackedObjectOffset = Vector3.Lerp(
            _framingTransposer.m_TrackedObjectOffset,
            Vector3.Lerp(_defaultTrackedObjectOffset, _closeTrackedObjectOffset, zoomAngleRatio),
            Time.deltaTime * _angleZoomSpeed);
    }
}
