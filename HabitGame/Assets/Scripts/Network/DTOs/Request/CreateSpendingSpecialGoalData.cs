using Newtonsoft.Json;

/// <summary>
/// 주간 예산 생성 시 함께 포함될 소비 습관 특수 목표 데이터
/// </summary>
public class CreateSpendingSpecialGoalData
{
    // 특수 목표의 제목 (예: "커피값 줄이기")
    [JsonProperty("title")]
    public string Title { get; set; }

    // 특수 목표의 상세 설명 및 조건 (예: "이번 주 카페 지출 3만원 이하로 유지")
    [JsonProperty("targetDescription")]
    public string TargetDescription { get; set; }

    // 목표 달성 시 지급될 골드량
    [JsonProperty("bonusGold")]
    public int BonusGold { get; set; }
}
