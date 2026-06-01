using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// 주간 지출 예산 정보 및 특수 목표 정보를 담은 응답 DTO
/// </summary>
public class SpendingBudgetResponse
{
    // 예산 항목의 고유 ID
    [JsonProperty("id")]
    public long Id { get; set; }

    // 유저 식별 ID
    [JsonProperty("userId")]
    public long UserId { get; set; }

    // 유저가 설정한 주간 예산 총액
    [JsonProperty("budgetAmount")]
    public int BudgetAmount { get; set; }

    // 예외 처리 이전 총 지출액
    [JsonProperty("rawTotalSpent")]
    public int RawTotalSpent { get; set; }

    // 예외 처리 금액 합계
    [JsonProperty("exceptionSpent")]
    public int ExceptionSpent { get; set; }

    // 현재까지 집계된 해당 주간의 총 지출액 (예외 처리된 금액 제외)
    [JsonProperty("totalSpent")]
    public int TotalSpent { get; set; }

    // 예산 적용 주간의 시작일
    [JsonProperty("weekStart")]
    public string WeekStart { get; set; }

    // 예산 적용 주간의 종료일
    [JsonProperty("weekEnd")]
    public string WeekEnd { get; set; }

    // 전체 예산 절약에 따른 보상 수령 여부
    [JsonProperty("rewardClaimed")]
    public bool RewardClaimed { get; set; }

    // 해당 주간 예산에 포함된 특수 목표들의 응답 데이터 목록
    [JsonProperty("specialGoals")]
    public List<SpendingSpecialGoalResponse> SpecialGoals { get; set; }

    // 예산 설정 생성 일시
    [JsonProperty("createdAt")]
    public string CreatedAt { get; set; }
}
