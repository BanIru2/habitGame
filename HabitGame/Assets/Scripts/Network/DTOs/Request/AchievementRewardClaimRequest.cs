using Newtonsoft.Json;

public class AchievementRewardClaimRequest
{
    [JsonProperty("userId")]
    public long UserId { get; set; }

    [JsonProperty("achievementId")]
    public string AchievementId { get; set; }
}