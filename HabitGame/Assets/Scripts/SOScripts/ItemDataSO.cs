using System;
using UnityEngine;

public enum ItemType
{
    Equipment,    // 장비
    Consumable,    // 소비
    Cosmetic    // 치장 (후순위)
}

[Serializable]
public class ItemUnlockCondition
{
    [Min(0)] public int requiredAttributeLevel;
    public AttributeType requiredAttribute;

    [Min(0)] public int requiredDailyStreak;
    [Min(0)] public int requiredWeeklyStreak;

    public bool HasAttributeRequirement => requiredAttributeLevel > 0;
    public bool HasDailyStreakRequirement => requiredDailyStreak > 0;
    public bool HasWeeklyStreakRequirement => requiredWeeklyStreak > 0;
}

public abstract class ItemDataSO : ScriptableObject
{
    public ItemType itemType;

    public string itemId;    // DB 연결 등 영어로 사용할 식별자
    public string displayName;    // 한국어로 된 아이템명
    public string description;
    public int cost;

    public Sprite icon;
    [Header("해금 조건 [없으면 0]")]
    public ItemUnlockCondition unlockCondition = new ItemUnlockCondition();
}
