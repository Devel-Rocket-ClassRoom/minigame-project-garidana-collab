using UnityEngine;

[CreateAssetMenu(fileName = "SoundData", menuName = "Sound/SoundData")]
public class SoundData : ScriptableObject
{
    [Header("BGM")]
    public AudioClip bgmMainTitle;
    public AudioClip bgmTown;
    public AudioClip bgmDesert;
    public AudioClip bgmForest;
    public AudioClip bgmOrcOutpost;
    public AudioClip bgmTomb;
    public AudioClip bgmBoss;

    [Header("UI")]
    public AudioClip buttonClick;
    public AudioClip chestOpen;
    public AudioClip waypointActivate;
    public AudioClip waypointUse;
    public AudioClip goldSpend;
    public AudioClip noGold;
    public AudioClip inventoryOpen;
    public AudioClip inventoryClose;
    public AudioClip chestNearbyLoop;
    public AudioClip optionMenuOpen;
    public AudioClip optionMenuClose;
    public AudioClip itemCollect;
    public AudioClip questUiOpen;
    public AudioClip questUiClose;
    public AudioClip swordEquip;
    public AudioClip shieldEquip;
    public AudioClip armorEquip;

    [Header("Player")]
    public AudioClip[] playerAttackVoices;
    public AudioClip[] swordSwings;
    public AudioClip[] playerHits;
    public AudioClip playerDeath;
    public AudioClip[] dashes;
    public AudioClip levelUp;
    public AudioClip heal;
    public AudioClip healRefill;
    public AudioClip[] footsteps;

    [Header("Monster")]
    public AudioClip monsterHit;
    public AudioClip monsterDeath;
}
