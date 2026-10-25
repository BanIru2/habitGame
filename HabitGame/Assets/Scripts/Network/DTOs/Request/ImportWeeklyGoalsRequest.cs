
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class ImportWeeklyGoalsRequest
{
    [JsonProperty("goals")]
    public List<ImportWeeklyGoalItem> Goals { get; set; }
        = new List<ImportWeeklyGoalItem>();
}

[Serializable]
public class ImportWeeklyGoalItem
{
    [JsonProperty("sourceGoalId")]
    public long SourceGoalId { get; set; }

    [JsonProperty("targetAmount")]
    public int TargetAmount { get; set; }
}
