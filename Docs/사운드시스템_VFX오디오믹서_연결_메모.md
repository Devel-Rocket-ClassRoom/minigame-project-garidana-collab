# VFX 이펙트 오디오 믹서 연결 메모

작성일: 2026-06-05
대상 프로젝트: Project Origin
대상 씬: `Assets/Scenes/SampleScene.unity`

이 문서는 다른 PC에서 `PlayerAttackUpgrade`에 연결된 VFX 이펙트 프리팹의 내장 사운드를 AudioMixer에 포함시키는 작업을 이어가기 위한 메모입니다.

---

## 1. 현재 구조 요약

`SoundManager`의 일반 SFX는 다음 구조로 AudioMixer에 연결되어 있습니다.

- 파일: `Assets/Scripts/Sound/SoundManager.cs`
- `Resources.Load<AudioMixer>("Audio/AudioMixer")`로 믹서 로드
- `FindMatchingGroups("SFX")`로 SFX 그룹 검색
- `_sfxSource.outputAudioMixerGroup = sfxGroups[0]`
- `PlaySFX()`는 `_sfxSource.PlayOneShot(clip)`으로 재생

하지만 `PlayerAttackUpgrade`에서 생성하는 VFX 이펙트는 `SoundManager`의 `_sfxSource`를 사용하지 않습니다.

`PlayerAttackUpgrade`는 현재 다음처럼 이펙트 프리팹을 직접 생성합니다.

```csharp
ParticleSystem effect = Instantiate(
    _currentStage.AttackEffectPrefab,
    position,
    rotation
);

effect.Play();
```

따라서 VFX 프리팹 안에 `AudioSource`가 들어 있고 `Play On Awake` 또는 자체 스크립트로 소리가 난다면, 그 소리는 별도 `AudioSource`에서 재생됩니다.

결론:

- `SoundManager`의 SFX 볼륨에 포함하려면 VFX 프리팹 안의 `AudioSource.outputAudioMixerGroup`도 `AudioMixer/SFX`로 지정해야 합니다.
- `SoundManager.PlaySFX()`에 등록된 클립만 AudioMixer에 들어가는 것이 아닙니다.
- 모든 `AudioSource`는 각자 `Output` 필드가 있어야 AudioMixer 그룹을 탑니다.

---

## 2. 현재 AudioMixer 위치와 그룹

AudioMixer 위치:

```text
Assets/Resources/Audio/AudioMixer.mixer
```

그룹 구조:

```text
Master
├── BGM
└── SFX
```

Exposed Parameter:

- `MasterVolume`
- `BGMVolume`
- `SFXVolume`

VFX 이펙트 내장 사운드는 `SFX` 그룹에 연결하는 것이 맞습니다.

---

## 3. 현재 씬에서 봐야 할 위치

씬:

```text
Assets/Scenes/SampleScene.unity
```

Hierarchy:

```text
SampleScene
└── Player
```

Inspector:

```text
Player
└── PlayerAttackUpgrade
    └── Attack Stages
        ├── Attack Effect Prefab
        └── Hit Effect Prefab
```

`PlayerAttackUpgrade`의 `_attackStages` 배열에 공격 단계별 VFX 프리팹이 연결되어 있습니다.

---

## 4. SampleScene에서 확인된 VFX 프리팹

현재 `SampleScene`의 Player 인스턴스에 연결된 VFX는 로컬 Variant 프리팹입니다.

Hit Effect 쪽:

```text
Assets/Prefabs/VFX/BloodExplosionSpiky Variant.prefab
Assets/Prefabs/VFX/Burst_sharp Variant.prefab
Assets/Prefabs/VFX/MagicNovaExplosionBlue Variant.prefab
Assets/Prefabs/VFX/ShadowExplosion Variant.prefab
Assets/Prefabs/VFX/ExplosionNovaSoftFire Variant.prefab
```

Attack Effect 쪽:

```text
Assets/Prefabs/VFX/SwordSlashThickBlue Variant.prefab
Assets/Prefabs/VFX/Slash_magic_once Variant.prefab
Assets/Prefabs/VFX/Slash_fire_once Variant.prefab
```

주의:

- 원본 `Assets/Imported/...` 프리팹을 직접 고치기보다 `Assets/Prefabs/VFX/... Variant.prefab` 쪽을 수정하는 것이 좋습니다.
- 현재 씬도 이미 로컬 Variant를 사용하고 있으므로, 프로젝트용 수정은 이 Variant에 적용하는 방향이 맞습니다.

---

## 5. Unity Editor에서 수동 연결하는 방법

1. `SampleScene`을 엽니다.
2. Hierarchy에서 `Player`를 선택합니다.
3. Inspector에서 `PlayerAttackUpgrade`를 찾습니다.
4. `Attack Stages` 배열을 펼칩니다.
5. 소리가 나는 `Attack Effect Prefab` 또는 `Hit Effect Prefab`을 클릭합니다.
6. 해당 VFX Variant 프리팹을 Prefab Mode로 엽니다.
7. 프리팹 안에서 `AudioSource`가 붙은 오브젝트를 찾습니다.
8. `AudioSource > Output` 필드에 `Assets/Resources/Audio/AudioMixer`의 `SFX` 그룹을 넣습니다.
9. 프리팹 Variant에 Apply 합니다.
10. Play Mode에서 옵션 메뉴의 SFX 볼륨을 줄였을 때 해당 이펙트 소리도 같이 줄어드는지 확인합니다.

확인 기준:

- SFX 볼륨 1.0: 이펙트 내장 사운드가 정상 재생됨
- SFX 볼륨 0.0 근처: 이펙트 내장 사운드가 거의 들리지 않음
- Master 볼륨 변경 시에도 같이 반응함

---

## 6. 왜 프리팹마다 직접 확인해야 하는가

`Assets/Prefabs/VFX`의 로컬 Variant 파일 자체에는 `AudioSource` 오버라이드가 직접 보이지 않을 수 있습니다.

이 경우 AudioSource는 원본 Imported 프리팹에서 상속된 컴포넌트일 수 있습니다.

Unity Editor에서는 Prefab Mode로 Variant를 열면 상속된 자식 오브젝트와 컴포넌트를 확인할 수 있습니다.

따라서 텍스트 파일 검색에서 `AudioSource`가 바로 보이지 않아도, Editor에서 Variant를 열어 실제 계층을 확인해야 합니다.

---

## 7. 더 안정적인 코드 방식

VFX 프리팹이 많아질 경우, 모든 프리팹의 `AudioSource Output`을 수동으로 지정하면 누락될 수 있습니다.

그럴 때는 `PlayerAttackUpgrade.PlayAttackEffect()`에서 이펙트를 생성한 직후, 생성된 이펙트 하위의 모든 `AudioSource`를 찾아 SFX 그룹으로 자동 연결하는 방법이 더 안정적입니다.

예상 방향:

```csharp
AudioSource[] audioSources = effect.GetComponentsInChildren<AudioSource>(true);
foreach (AudioSource source in audioSources)
{
    source.outputAudioMixerGroup = sfxMixerGroup;
}
```

다만 이 방식은 `SoundManager`가 `SFX` AudioMixerGroup을 외부에 제공하는 API가 필요합니다.

예상 추가 API:

```csharp
public AudioMixerGroup GetSfxMixerGroup()
```

또는:

```csharp
public void RouteAudioSourcesToSfx(GameObject root)
```

현재 단계에서는 코드 수정 없이 Unity Editor에서 VFX Variant의 `AudioSource Output`을 `SFX`로 지정하는 방식이 가장 작은 작업입니다.

---

## 8. 다음 작업 지시 예시

다른 PC에서 이어서 작업할 때는 다음처럼 요청하면 됩니다.

```text
Docs/사운드시스템_VFX오디오믹서_연결_메모.md를 읽고,
SampleScene의 PlayerAttackUpgrade에 연결된 VFX Variant 프리팹들의 AudioSource Output을 AudioMixer/SFX로 연결해야 하는지 확인해줘.
아직 코드는 수정하지 말고, 어떤 프리팹에 AudioSource가 있는지 먼저 알려줘.
```

코드 방식으로 진행하고 싶을 때:

```text
Docs/사운드시스템_VFX오디오믹서_연결_메모.md 기준으로,
VFX 이펙트 생성 직후 하위 AudioSource들을 SFX AudioMixerGroup으로 자동 라우팅하는 구조를 제안해줘.
수정 전에는 어떤 파일을 고칠지 먼저 말해줘.
```

---

## 9. 주의 사항

- 사용자가 명시적으로 구현을 요청하기 전까지는 코드를 수정하지 말 것.
- 원본 Imported VFX 프리팹보다 `Assets/Prefabs/VFX` 아래 로컬 Variant를 우선 확인할 것.
- `SoundManager`의 SFX 볼륨이 적용되는 것은 `_sfxSource`뿐이므로, 외부 프리팹의 `AudioSource`는 별도로 Mixer Group 연결이 필요함.
- VFX 프리팹이 `PlayClipAtPoint` 같은 방식으로 런타임 AudioSource를 새로 만든다면, 프리팹 Output 지정만으로는 부족할 수 있음.
