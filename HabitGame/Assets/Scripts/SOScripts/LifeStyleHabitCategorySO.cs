using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// *** DB연결 시 enum -> string 변환 필요 ***
// 클래스 내부 string CategoryID로 변환 기능
public enum HabitCategory
{
    Physical,    // 신체활동 (불)
    Biorhythm,   // 생체리듬 (물)
    Environment, // 환경관리 (풀)
    SelfDev      // 자기계발 (오로라)
}   

public enum AttributeType
{
    None, Fire, Water, Grass, Aurora
}

public enum StatType
{
    None, ATK, DEF, HP, SPD, CRIT
}

[CreateAssetMenu(menuName = "Habit/LifeStyleCategory")]

public class LifeStyleHabitCategorySO : ScriptableObject
{
    public HabitCategory category;      // 드롭다운으로 선택
    // 생활습관 카테고리에 따라 상승할 특성 및 스텟 자동 결정
    public AttributeType Attribute => category switch
    {
        HabitCategory.Physical => AttributeType.Fire,
        HabitCategory.Biorhythm => AttributeType.Water,
        HabitCategory.Environment => AttributeType.Grass,
        HabitCategory.SelfDev => AttributeType.Aurora,
        _ => throw new System.ArgumentOutOfRangeException()
    };
    public StatType PrimaryStat => category switch
    {
        HabitCategory.Physical => StatType.HP,
        HabitCategory.Biorhythm => StatType.HP,
        HabitCategory.Environment => StatType.DEF,
        HabitCategory.SelfDev => StatType.ATK,
        _ => throw new System.ArgumentOutOfRangeException()
    };
    public StatType SecondaryStat => category switch
    {
        HabitCategory.Physical => StatType.ATK,
        HabitCategory.Biorhythm => StatType.DEF,
        HabitCategory.Environment => StatType.SPD,
        HabitCategory.SelfDev => StatType.CRIT,
        _ => throw new System.ArgumentOutOfRangeException()
    };

    // 생활습관 보상 지급 식; 기본 수치x난이도보정치x달성률 의 기본 수치값
    // 결정되면 이것도 종속적으로 결정되도록 수정은 가능하나 현재는 인스펙터에서 수정하도록 열어둠
    public float attributStatMultiplier;    // 특성 기본 수치
    public float primaryStatMultiplier;    // 스탯1 기본 수치
    public float secondaryStatMultiplier;    // 스탯2 기본 수치

    // 표기되는 항목명 종속
    public string DisplayName => category switch
    {
        HabitCategory.Physical => "신체활동",
        HabitCategory.Biorhythm => "생체리듬",
        HabitCategory.Environment => "환경관리",
        HabitCategory.SelfDev => "자기계발",
        _ => throw new System.ArgumentOutOfRangeException()
    };
    public Sprite icon;

    public string CategoryId => category switch
    {
        HabitCategory.Physical => "physical",
        HabitCategory.Biorhythm => "biorhythm",
        HabitCategory.Environment => "environment",
        HabitCategory.SelfDev => "selfdev",
        _ => throw new System.ArgumentOutOfRangeException()
    };
}
