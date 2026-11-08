using Newtonsoft.Json;

public class CreateSpendingTransactionRequest
{
    [JsonProperty("idempotencyKey")]
    public string IdempotencyKey { get; set; }

    [JsonProperty("amount")]
    public int Amount { get; set; }

    [JsonProperty("merchantName")]
    public string MerchantName { get; set; }

    [JsonProperty("category")]
    public string Category { get; set; }

    [JsonProperty("transactionType")]
    public string TransactionType { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("isException")]
    public bool IsException { get; set; }

    [JsonProperty("exceptionReason")]
    public string ExceptionReason { get; set; }

    [JsonProperty("recordedAt")]
    public string RecordedAt { get; set; }
}