using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public class CreateSpendingGoalRequest
{
    [JsonProperty("userId")]
    public long UserId { get; set; }

    [JsonProperty("goalName")]
    public string GoalName { get; set; }

    [JsonProperty("limitAmount")]
    public int LimitAmount { get; set; }

    [JsonProperty("rewardGold")]
    public int RewardGold { get; set; }
}
