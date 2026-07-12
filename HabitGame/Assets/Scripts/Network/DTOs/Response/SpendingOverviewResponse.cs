using System.Collections.Generic;
using Newtonsoft.Json;

public class SpendingOverviewResponse
{
    [JsonProperty("budgetId")]
    public long BudgetId { get; set; }

    [JsonProperty("budgetAmount")]
    public int BudgetAmount { get; set; }

    [JsonProperty("currentSpent")]
    public int CurrentSpent { get; set; }

    [JsonProperty("period")]
    public string Period { get; set; }

    [JsonProperty("usageRate")]
    public int UsageRate { get; set; }

    [JsonProperty("expectedGold")]
    public int ExpectedGold { get; set; }

    [JsonProperty("goals")]
    public List<SpendingSpecialGoalResponse> Goals { get; set; }

    // 이번 주 예산 상태
    [JsonProperty("budget")]
    public SpendingBudgetResponse Budget { get; set; }

    // DB에 저장 된 이번 주 거래 내역 리스트
    [JsonProperty("transactions")]
    public List<SpendingTransactionResponse> Transactions { get; set; }

    // 마지막 동기화 시기
    [JsonProperty("lastSyncedAt")]
    public string LastSyncedAt { get; set; }
}
