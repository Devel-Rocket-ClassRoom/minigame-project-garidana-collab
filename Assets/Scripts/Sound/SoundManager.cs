using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    // AudioMixer Exposed Parameter 이름과 일치해야 함
    private const string MasterVolumeParam = "MasterVolume";
    private const string BGMVolumeParam    = "BGMVolume";
    private const string SFXVolumeParam    = "SFXVolume";

    // PlayerPrefs 저장 키
    private const string MasterVolumePref = "Pref_MasterVolume";
    private const string BGMVolumePref    = "Pref_BGMVolume";
    private const string SFXVolumePref    = "Pref_SFXVolume";

    private const float DefaultVolume = 0.8f;
    private const float DefaultBgmFadeDuration = 1f;

    // Resources/Audio/ 폴더 경로
    private const string AudioMixerPath = "Audio/AudioMixer";
    private const string SoundDataPath  = "Audio/SoundData";

    private AudioMixer _audioMixer;
    private SoundData  _soundData;
    private AudioSource _bgmSourceA;
    private AudioSource _bgmSourceB;
    private AudioSource _currentBgmSource;
    private AudioSource _nextBgmSource;
    private AudioSource _sfxSource;
    private Coroutine _bgmFadeCoroutine;
    private BGMType? _currentBgmType;

    // Awake보다 먼저 할당되도록 static으로 보관
    private static AudioMixer _pendingMixer;
    private static SoundData  _pendingData;

    public enum SFXType
    {
        ButtonClick,
        ChestOpen,
        WaypointActivate,
        GoldSpend,
        NoGold,
        PlayerAttackVoice,
        SwordSwing,
        PlayerHit,
        PlayerDeath,
        Dash,
        LevelUp,
        Footstep,
        MonsterHit,
        MonsterDeath
    }

    public enum BGMType
    {
        MainTitle,
        Town,
        Desert,
        Forest,
        OrcBase,
        Tomb,
        Boss
    }

    // ──────────────────────────────────────────────
    // 초기화
    // ──────────────────────────────────────────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (Instance != null)
        {
            return;
        }

        AudioMixer mixer = Resources.Load<AudioMixer>(AudioMixerPath);
        SoundData data   = Resources.Load<SoundData>(SoundDataPath);

        Debug.Log($"[SoundManager] Initialize - mixer: {(mixer != null ? "OK" : "NULL")}, data: {(data != null ? "OK" : "NULL")}");

        if (mixer == null)
        {
            Debug.LogWarning("[SoundManager] AudioMixer를 찾을 수 없습니다. Resources/Audio/ 폴더를 확인하세요.");
        }

        if (data == null)
        {
            Debug.LogWarning("[SoundManager] SoundData를 찾을 수 없습니다. Resources/Audio/ 폴더를 확인하세요.");
        }

        // AddComponent → Awake 즉시 실행되므로 static에 먼저 저장
        _pendingMixer = mixer;
        _pendingData  = data;

        GameObject go = new GameObject("[SoundManager]");
        DontDestroyOnLoad(go);
        go.AddComponent<SoundManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _audioMixer = _pendingMixer;
        _soundData  = _pendingData;

        Debug.Log($"[SoundManager] Awake - mixer: {(_audioMixer != null ? "OK" : "NULL")}, data: {(_soundData != null ? "OK" : "NULL")}");

        SetupAudioSources();
        LoadVolumes();
    }

    private void SetupAudioSources()
    {
        _bgmSourceA = CreateBgmSource();
        _bgmSourceB = CreateBgmSource();
        _currentBgmSource = _bgmSourceA;
        _nextBgmSource = _bgmSourceB;

        _sfxSource           = gameObject.AddComponent<AudioSource>();
        _sfxSource.loop      = false;
        _sfxSource.playOnAwake = false;

        // AudioMixer 그룹 연결
        if (_audioMixer != null)
        {
            AudioMixerGroup[] bgmGroups = _audioMixer.FindMatchingGroups("BGM");
            AudioMixerGroup[] sfxGroups = _audioMixer.FindMatchingGroups("SFX");

            if (bgmGroups.Length > 0)
            {
                _bgmSourceA.outputAudioMixerGroup = bgmGroups[0];
                _bgmSourceB.outputAudioMixerGroup = bgmGroups[0];
            }

            if (sfxGroups.Length > 0) _sfxSource.outputAudioMixerGroup = sfxGroups[0];
        }
    }

    private AudioSource CreateBgmSource()
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.loop = true;
        source.playOnAwake = false;
        source.volume = 0f;
        return source;
    }

    // ──────────────────────────────────────────────
    // BGM
    // ──────────────────────────────────────────────

    public void PlayBGM(BGMType type)
    {
        PlayBGM(type, DefaultBgmFadeDuration);
    }

    public void PlayBGM(BGMType type, float fadeDuration)
    {
        AudioClip clip = GetBGMClip(type);
        if (clip == null)
        {
            return;
        }

        if (_currentBgmType == type && _currentBgmSource.clip == clip && _currentBgmSource.isPlaying)
        {
            return;
        }

        if (_bgmFadeCoroutine != null)
        {
            StopCoroutine(_bgmFadeCoroutine);
        }

        _bgmFadeCoroutine = StartCoroutine(FadeToBGM(type, clip, Mathf.Max(0f, fadeDuration)));
    }

    public void StopBGM()
    {
        if (_bgmFadeCoroutine != null)
        {
            StopCoroutine(_bgmFadeCoroutine);
            _bgmFadeCoroutine = null;
        }

        StopBgmSource(_bgmSourceA);
        StopBgmSource(_bgmSourceB);
        _currentBgmType = null;
        _currentBgmSource = _bgmSourceA;
        _nextBgmSource = _bgmSourceB;
    }

    private IEnumerator FadeToBGM(BGMType type, AudioClip clip, float fadeDuration)
    {
        AudioSource fromSource = _currentBgmSource;
        AudioSource toSource = _nextBgmSource;

        toSource.clip = clip;
        toSource.volume = 0f;
        toSource.Play();

        if (fadeDuration <= 0f)
        {
            if (fromSource != null)
            {
                StopBgmSource(fromSource);
            }

            toSource.volume = 1f;
            SwapBgmSources(type);
            yield break;
        }

        float fromStartVolume = fromSource != null && fromSource.isPlaying ? fromSource.volume : 0f;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);

            if (fromSource != null)
            {
                fromSource.volume = Mathf.Lerp(fromStartVolume, 0f, t);
            }

            toSource.volume = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }

        if (fromSource != null)
        {
            StopBgmSource(fromSource);
        }

        toSource.volume = 1f;
        SwapBgmSources(type);
    }

    private void SwapBgmSources(BGMType type)
    {
        AudioSource previousSource = _currentBgmSource;
        _currentBgmSource = _nextBgmSource;
        _nextBgmSource = previousSource;
        _currentBgmType = type;
        _bgmFadeCoroutine = null;
    }

    private static void StopBgmSource(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        source.Stop();
        source.clip = null;
        source.volume = 0f;
    }

    private AudioClip GetBGMClip(BGMType type)
    {
        if (_soundData == null)
        {
            return null;
        }

        return type switch
        {
            BGMType.MainTitle => _soundData.bgmMainTitle,
            BGMType.Town      => _soundData.bgmTown,
            BGMType.Desert    => _soundData.bgmDesert,
            BGMType.Forest    => _soundData.bgmForest,
            BGMType.OrcBase=> _soundData.bgmOrcOutpost,
            BGMType.Tomb      => _soundData.bgmTomb,
            BGMType.Boss      => _soundData.bgmBoss,
            _                 => null
        };
    }

    // ──────────────────────────────────────────────
    // SFX
    // ──────────────────────────────────────────────

    public void PlaySFX(SFXType type)
    {
        AudioClip clip = GetSFXClip(type);
        if (clip == null)
        {
            Debug.LogWarning($"[SoundManager] PlaySFX 클립 없음: {type} (SoundData에 클립이 할당됐는지 확인하세요)");
            return;
        }

        Debug.Log($"[SoundManager] PlaySFX: {type}");
        _sfxSource.PlayOneShot(clip);
    }

    private AudioClip GetSFXClip(SFXType type)
    {
        if (_soundData == null)
        {
            return null;
        }

        return type switch
        {
            SFXType.ButtonClick      => _soundData.buttonClick,
            SFXType.ChestOpen        => _soundData.chestOpen,
            SFXType.WaypointActivate => _soundData.waypointActivate,
            SFXType.GoldSpend        => _soundData.goldSpend,
            SFXType.NoGold           => _soundData.noGold,
            SFXType.PlayerAttackVoice=> GetRandomClip(_soundData.playerAttackVoices),
            SFXType.SwordSwing       => GetRandomClip(_soundData.swordSwings),
            SFXType.PlayerHit        => _soundData.playerHit,
            SFXType.PlayerDeath      => _soundData.playerDeath,
            SFXType.Dash             => _soundData.dash,
            SFXType.LevelUp          => _soundData.levelUp,
            SFXType.Footstep         => _soundData.footstep,
            SFXType.MonsterHit       => _soundData.monsterHit,
            SFXType.MonsterDeath     => _soundData.monsterDeath,
            _                        => null
        };
    }

    // ──────────────────────────────────────────────
    // 볼륨 제어 (0~1 → dB 변환)
    // ──────────────────────────────────────────────

    public void SetMasterVolume(float value)
    {
        SetMixerVolume(MasterVolumeParam, value);
        PlayerPrefs.SetFloat(MasterVolumePref, value);
    }

    public void SetBGMVolume(float value)
    {
        SetMixerVolume(BGMVolumeParam, value);
        PlayerPrefs.SetFloat(BGMVolumePref, value);
    }

    public void SetSFXVolume(float value)
    {
        SetMixerVolume(SFXVolumeParam, value);
        PlayerPrefs.SetFloat(SFXVolumePref, value);
    }

    public float GetMasterVolume() => PlayerPrefs.GetFloat(MasterVolumePref, DefaultVolume);
    public float GetBGMVolume()    => PlayerPrefs.GetFloat(BGMVolumePref,    DefaultVolume);
    public float GetSFXVolume()    => PlayerPrefs.GetFloat(SFXVolumePref,    DefaultVolume);

    private void SetMixerVolume(string paramName, float value)
    {
        if (_audioMixer == null)
        {
            return;
        }

        // 0 이하 방지 후 dB 변환
        float db = Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f;
        bool success = _audioMixer.SetFloat(paramName, db);
        if (!success)
        {
            Debug.LogWarning($"[SoundManager] AudioMixer SetFloat 실패 - 파라미터 이름 불일치: '{paramName}' (AudioMixer Exposed Parameters 이름을 확인하세요)");
        }
    }

    private void LoadVolumes()
    {
        SetMasterVolume(GetMasterVolume());
        SetBGMVolume(GetBGMVolume());
        SetSFXVolume(GetSFXVolume());
    }

    private AudioClip GetRandomClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
        {
            return null;
        }

        return clips[UnityEngine.Random.Range(0, clips.Length)];
    }
}
