using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// 주간 지출 예산 및 특수 목표 설정을 위한 DTO
/// </summary>
public class CreateSpendingBudgetRequest
{
    // 유저가 설정한 주간 예산 총액
    [JsonProperty("budgetAmount")]
    public int BudgetAmount { get; set; }

    // 예산 적용 시작 날짜
    [JsonProperty("weekStart")]
    public string WeekStart { get; set; }

    // 예산 적용 종료 날짜
    [JsonProperty("weekEnd")]
    public string WeekEnd { get; set; }

    // 주간 예산과 함께 설정할 특수 목표 리스트
    [JsonProperty("specialGoals")]
    public List<CreateSpendingSpecialGoalData> SpecialGoals { get; set; }
}
