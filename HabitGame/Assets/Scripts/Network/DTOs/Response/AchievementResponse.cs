using Newtonsoft.Json;

public class AchievementResponse
{
    [JsonProperty("achievementId")]
    public string AchievementId { get; set; }

    [JsonProperty("currentCount")]
    public int CurrentCount { get; set; }

    [JsonProperty("isClaimed")]
    public bool IsClaimed { get; set; }
}