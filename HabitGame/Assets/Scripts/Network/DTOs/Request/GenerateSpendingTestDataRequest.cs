
using Newtonsoft.Json;

public class GenerateSpendingTestDataRequest
{
    [JsonProperty("count")]
    public int Count { get; set; }

    [JsonProperty("startDate")]
    public string StartDate { get; set; }

    [JsonProperty("endDate")]
    public string EndDate { get; set; }
}
