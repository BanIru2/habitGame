using Newtonsoft.Json;

/// <summary>
/// 개별 상세 지출 내역(결제 건) 정보를 담은 응답 DTO
/// </summary>
public class SpendingTransactionResponse
{
    // 지출 내역의 고유 ID
    [JsonProperty("id")]
    public long Id { get; set; }

    // 해당 지출이 포함된 주간 예산의 ID
    [JsonProperty("budgetId")]
    public long BudgetId { get; set; }

    // 실제 결제 금액
    [JsonProperty("amount")]
    public int Amount { get; set; }

    // 가맹점 이름 (예: "스타벅스", "배달의민족")
    [JsonProperty("merchantName")]
    public string MerchantName { get; set; }

    // 결제 항목에 대한 상세 설명 (유저 메모 등)
    [JsonProperty("description")]
    public string Description { get; set; }

    // 주간 예산 합산에서 제외(예외) 처리되었는지 여부
    [JsonProperty("isException")]
    public bool IsException { get; set; }

    // 예외 처리된 경우 그 사유
    [JsonProperty("exceptionReason")]
    public string ExceptionReason { get; set; }

    // 실제 결제가 발생한 일시
    [JsonProperty("recordedAt")]
    public string RecordedAt { get; set; }

    // 외부 결제 데이터가 앱으로 동기화된 일시
    [JsonProperty("syncedAt")]
    public string SyncedAt { get; set; }
}
