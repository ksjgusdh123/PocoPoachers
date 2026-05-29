using UnityEngine;

[CreateAssetMenu(fileName = "ArmorData", menuName = "Game/ArmorData")]
public class ArmorData : ScriptableObject
{
    public int itemId;

    [Header("방어구 스탯")]
    public float defense;
    public float maxHpBonus;
    public float moveSpeedMultiplier = 1f;
}
