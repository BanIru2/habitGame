using Newtonsoft.Json;

/// <summary>
/// 생활 습관 목표 달성 시 보상 지급 동작을 위한 응답 DTO
/// </summary>
public class HabitRewardClaimResponse
{
    [JsonProperty("userId")]
    public long UserId { get; set; }

    // 대상 습관 목표 Id (HabitRecordResponse.Id와 비교하여 어떤 목표에 대한 보상인지 확인)
    [JsonProperty("recordId")]
    public long RecordId { get; set; }

    [JsonProperty("goalId")]
    public long GoalId { get; set; }

    [JsonProperty("categoryId")]
    public string CategoryId { get; set; }

    // 특성 경험치 증가량
    [JsonProperty("attributeExpReward")]
    public int EarnedAttributeExp { get; set; }

    // 증가 특성 종류
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

    // 스탯 증가량1
    [JsonProperty("earnedPrimaryStatExp")]
    public int EarnedPrimaryStatExp { get; set; }

    // 증가 스탯 종류1
    [JsonProperty("primaryStatType")]
    public string PrimaryStatType { get; set; }

    // 스탯 증가량2
    [JsonProperty("earnedSecondaryStatExp")]
    public int EarnedSecondaryStatExp { get; set; }

    // 증가 스탯 종류2
    [JsonProperty("secondaryStatType")]
    public string SecondaryStatType { get; set; }

    // 보상 적용 후 캐릭터 전체 상태
    [JsonProperty("updatedCharacter")]
    public CharacterResponse UpdatedCharacter { get; set; }

    // 중복 수령 방지 플래그
    [JsonProperty("isRewardClaimed")]
    public bool RewardClaimed { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }
}
