using Newtonsoft.Json;

/// <summary>
/// 특정 지출 내역을 예외 항목(주간 예산 계산 제외)으로 처리하기 위한 DTO
/// </summary>
public class UpdateSpendingExceptionRequest
{
    [JsonProperty("transactionId")]
    public long TransactionId { get; set; }

    // 예외 처리 여부 (true: 예외 설정, false: 예외 해제)
    [JsonProperty("isException")]
    public bool IsException { get; set; }

    // 예외 처리 사유 (예: 업무 지출, 경조사 등)
    [JsonProperty("exceptionReason")]
    public string ExceptionReason { get; set; }
}
