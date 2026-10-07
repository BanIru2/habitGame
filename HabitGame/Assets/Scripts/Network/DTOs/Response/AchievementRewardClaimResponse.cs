using Newtonsoft.Json;

public class AchievementRewardClaimResponse
{
    [JsonProperty("achievementId")]
    public string AchievementId { get; set; }

    // 메인 보상: 획득한 칭호 ID (없을 경우 null)
    [JsonProperty("rewardTitleId")]
    public string RewardTitleId { get; set; }
}
