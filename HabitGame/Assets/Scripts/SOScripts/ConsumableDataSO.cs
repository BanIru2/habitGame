using UnityEngine;


public enum ItemEffectType
{
    BattleStatUp,    // 전투 일회성 스탯 상승
    PermanentStatUp,   // 영구적 스탯 상승
    AttributeLevelUp,    // 영구적 특성 레벨 상승
    ProtectStreak   // 습관 연속 달성 방어권
}

public enum HabitDomainType
{
    None,
    LifeStyle,
    Spending
}

public enum ItemUseTiming
{
    BattlePreparation,    // 전투 준비 단계 사용
    OutOfBattle    // 그외 상시 사용
}

[CreateAssetMenu(menuName = "Item/Consumable Data", fileName = "ConsumableData")]
public class ConsumableDataSO : ItemDataSO
{
    [Header("아이템 사용 시점 [전투 전 / 상시] ")]
    public ItemUseTiming useTiming;
    [Header("아이템 효과")]
    [Tooltip("일회성 스탯 상승 / 영구적 스탯 상승 / 영구적 특성 경험치 상승 / 습관 연속 달성 방어")]
    public ItemEffectType effectType;

    // 세 항목 중 해당하는 속성 외에는 None 선택
    [Header("해당하는 속성 외 None으로 설정")]
    public StatType targetStat;
    public HabitDomainType targetHabitDomain;
    public AttributeType targetAttribute;
    [Header("아이템 사용 시 상승 수치 [방어권은 1로 설정할 것]")]
    public float value; // 증가량. 방어권은 1로 설정하여 1회분 방어 표현
    [Header("최대 개수 : 99개로 고정 (임시)")]
    public int maxStack;

    // ItemType 항상 Consumable로 유지
    private void OnValidate()
    {
        itemType = ItemType.Consumable;
        maxStack = 99;    // 모든 아이템 최대 개수 99개로 설정 (임의)
    }
}
