using UnityEngine;
using System.Collections.Generic;

public class EquipmentManager : MonoBehaviour
{
    [Header("Slots")]
    [SerializeField] private Transform _rightHandSlot;
    [SerializeField] private Transform _leftHandSlot;

    [Header("Equipment Lists")]
    [SerializeField] private List<GameObject> _swordPrefabs;
    [SerializeField] private List<GameObject> _shieldPrefabs;

    [Header("Offsets")]
    [SerializeField] private Vector3 _swordPosOffset = Vector3.zero;
    [SerializeField] private Vector3 _swordRotOffset = Vector3.zero;
    [SerializeField] private Vector3 _shieldPosOffset = new Vector3(-0.03f, -0.04f, 0.07f);
    [SerializeField] private Vector3 _shieldRotOffset = Vector3.zero;

    private int _currentSwordIndex = 0;
    private int _currentShieldIndex = 0;

    private GameObject _currentSword;
    private GameObject _currentShield;

    private PlayerInputReader _inputReader;

    private void Awake()
    {
        _inputReader = GetComponent<PlayerInputReader>();
    }

    private void Start()
    {
        EquipCurrent();
    }

    private void Update()
    {
        if (_inputReader == null) return;

        if (_inputReader.PreviousRequested)
        {
            SwitchSword(1);
            _inputReader.UsePreviousInput();
        }
        
        if (_inputReader.NextRequested)
        {
            SwitchShield(1);
            _inputReader.UseNextInput();
        }
    }

    public void SwitchSword(int direction)
    {
        if (_swordPrefabs.Count <= 1) return;

        _currentSwordIndex = (_currentSwordIndex + direction + _swordPrefabs.Count) % _swordPrefabs.Count;
        EquipSword(_currentSwordIndex);
    }

    public void SwitchShield(int direction)
    {
        if (_shieldPrefabs.Count <= 1) return;

        _currentShieldIndex = (_currentShieldIndex + direction + _shieldPrefabs.Count) % _shieldPrefabs.Count;
        EquipShield(_currentShieldIndex);
    }

    public void EquipCurrent()
    {
        if (_swordPrefabs.Count > 0) EquipSword(_currentSwordIndex);
        if (_shieldPrefabs.Count > 0) EquipShield(_currentShieldIndex);
    }

    private void EquipSword(int index)
    {
        if (_currentSword != null) Destroy(_currentSword);
        if (index < 0 || index >= _swordPrefabs.Count || _swordPrefabs[index] == null) return;

        _currentSword = Instantiate(_swordPrefabs[index], _rightHandSlot);
        _currentSword.transform.localPosition = _swordPosOffset;
        _currentSword.transform.localRotation = Quaternion.Euler(_swordRotOffset);
    }
    private void EquipShield(int index)
    {
        if (_currentShield != null) Destroy(_currentShield);
        if (index < 0 || index >= _shieldPrefabs.Count || _shieldPrefabs[index] == null) return;

        _currentShield = Instantiate(_shieldPrefabs[index], _leftHandSlot);
        _currentShield.transform.localPosition = _shieldPosOffset;
        _currentShield.transform.localRotation = Quaternion.Euler(_shieldRotOffset);
    }
    }
