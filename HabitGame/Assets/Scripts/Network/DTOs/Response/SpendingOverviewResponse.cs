using System.Collections.Generic;
using Newtonsoft.Json;

public class SpendingOverviewResponse
{
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