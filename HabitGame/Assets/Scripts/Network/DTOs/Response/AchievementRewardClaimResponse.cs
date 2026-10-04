using Newtonsoft.Json;

public class AchievementRewardClaimResponse
{
    [JsonProperty("achievementId")]
    public string AchievementId { get; set; }

    [JsonProperty("isClaimed")]
    public bool IsClaimed { get; set; }

    // ¸ÞÀÎ º¸»ó: È¹µæÇÑ ÄªÈ£ ID
    [JsonProperty("rewardTitleId")]
    public string RewardTitleId { get; set; }
}