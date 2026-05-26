using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MonsterData", menuName = "Scriptable Objects/MonsterData")]
public class MonsterData : ScriptableObject
{
    [Header("Stats")]
    public float maxHp;
    public float attackDamage;
    public float moveSpeed;
    public float detectionRange;
    public float attackRange;
    public float attackCooldown;

    [Header("기본 드롭 (모든 몬스터 공통)")]
    public int goldMin;
    public int goldMax;
    public int expReward;

    // 몬스터 사냥 퀘스트용 몬스터 targetId;
    [Header("Quest")]
    public string questTargetId;

    [Header("퀘스트 아이템 드롭 (해당 구역 몬스터만 설정)")]
    public List<DropEntry> questDrops; // 비워둘 시 드롭 X
}
