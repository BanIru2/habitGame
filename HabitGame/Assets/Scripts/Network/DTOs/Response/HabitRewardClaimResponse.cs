using Newtonsoft.Json;

/// <summary>
/// 습관 달성 보상 수령 결과 DTO
/// </summary>
public class HabitRewardClaimResponse
{
    [JsonProperty("userId")]
    public long UserId { get; set; }

    [JsonProperty("recordId")]
    public long RecordId { get; set; }

    [JsonProperty("goalId")]
    public long GoalId { get; set; }

    [JsonProperty("categoryId")]
    public string CategoryId { get; set; }

    [JsonProperty("attributeExpReward")]
    public int EarnedAttributeExp { get; set; }

    [JsonProperty("attributeType")]
    public string AttributeType { get; set; }

    [JsonProperty("achievementRate")]
    public double AchievementRate { get; set; }

    [JsonProperty("goldReward")]
    public int GoldReward { get; set; }

    [JsonProperty("attributeLevel")]
    public int AttributeLevel { get; set; }

    [JsonProperty("totalGold")]
    public int TotalGold { get; set; }

    [JsonProperty("totalAttributeExp")]
    public int TotalAttributeExp { get; set; }

    [JsonProperty("isRewardClaimed")]
    public bool RewardClaimed { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }
}
