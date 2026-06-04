using UnityEngine;

[CreateAssetMenu(fileName = "SoundData", menuName = "Sound/SoundData")]
public class SoundData : ScriptableObject
{
    [Header("BGM")]
    public AudioClip bgmMainTitle;
    public AudioClip bgmGameplay;
    public AudioClip bgmBoss;

    [Header("UI")]
    public AudioClip buttonClick;
    public AudioClip chestOpen;
    public AudioClip waypointActivate;
    public AudioClip goldSpend;
    public AudioClip noGold;

    [Header("Player")]
    public AudioClip playerAttackVoice;
    public AudioClip swordSwing;
    public AudioClip playerHit;
    public AudioClip playerDeath;
    public AudioClip dash;
    public AudioClip levelUp;
    public AudioClip footstep;

    [Header("Monster")]
    public AudioClip monsterHit;
    public AudioClip monsterDeath;
}
