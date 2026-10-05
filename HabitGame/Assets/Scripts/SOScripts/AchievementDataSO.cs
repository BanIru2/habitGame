using UnityEngine;

// 업적 대분류
public enum AchievementCategory
{
    Habit,       // 습관 관련 (인증 횟수, 연속 달성 등)
    Growth,      // 성장 관련 (속성 레벨, 장비 강화 등)
    Activity,    // 활동 관련 (가챠, 골드 사용, 출석 등)
    Battle       // PvP 관련
}

// 업적 추적 대상 타입
public enum AchievementType
{
    HabitVerifyCount,   // 특정 속성 습관 인증 횟수
    AttributeLevel,     // 속성 레벨 도달
    GachaCount,         // 가챠 실행 횟수
    EquipmentEnhance,   // 장비 강화 횟수
    PvpWinCount         // PvP 승리 횟수
}

[CreateAssetMenu(menuName = "Character/AchievementData", fileName = "AchievementData")]
public class AchievementDataSO : ScriptableObject
{
    [Header("기본 정보")]
    public string achievementId;       // 서버 연동용 고유 ID (예: "ACH_FIRE_HABIT_10")
    public string content;             // 업적 내용

    [Header("카테고리 & 속성")]
    public AchievementCategory category;
    public AttributeType attribute; // 게이지 색상 결정용 (Fire, Water, Grass, Aurora, None)

    [Header("달성 조건")]
    public AchievementType trackingType; // 무엇을 추적할 것인가
    [Min(1)]
    public int targetCount;         // 목표 수치

    [Header("보상")]
    public TitleDataSO rewardTitle;      // 보상 칭호
}