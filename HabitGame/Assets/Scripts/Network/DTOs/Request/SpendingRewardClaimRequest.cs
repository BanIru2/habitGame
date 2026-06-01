using Newtonsoft.Json;

/// <summary>
/// 소비 습관 보상 수령 요청을 위한 DTO
/// </summary>
public class SpendingRewardClaimRequest
{
    // 보상을 수령할 예산(Budget)의 고유 ID
    [JsonProperty("budgetId")]
    public long BudgetId { get; set; }
}
