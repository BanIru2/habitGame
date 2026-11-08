
using System.Collections.Generic;
using Newtonsoft.Json;

public class GenerateSpendingTestDataResponse
{
    [JsonProperty("success")]
    public bool Success { get; set; }

    [JsonProperty("generatedCount")]
    public int GeneratedCount { get; set; }

    [JsonProperty("transactionIds")]
    public List<long> TransactionIds { get; set; }

    [JsonProperty("transactions")]
    public List<SpendingTransactionResponse> Transactions { get; set; }
}
