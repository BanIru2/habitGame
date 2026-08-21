using Newtonsoft.Json;

public class DailyPvpLimitResponse
{
    [JsonProperty("remainingCount")]
    public int RemainingCount { get; set; }
}
