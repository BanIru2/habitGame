using Newtonsoft.Json;

public class WeeklyReportResponse
{
    // 조회 주간
    [JsonProperty("weekStart")]
    public string WeekStart { get; set; }

    [JsonProperty("weekEnd")]
    public string WeekEnd { get; set; }


    // =========================
    // 생활습관
    // =========================

    [JsonProperty("habitAchievementRate")]
    public int HabitAchievementRate { get; set; }

    [JsonProperty("completedHabitCount")]
    public int CompletedHabitCount { get; set; }

    [JsonProperty("totalHabitCount")]
    public int TotalHabitCount { get; set; }


    // =========================
    // 소비습관
    // =========================

    [JsonProperty("weeklyBudget")]
    public int WeeklyBudget { get; set; }

    [JsonProperty("weeklySpent")]
    public int WeeklySpent { get; set; }

    [JsonProperty("budgetUsageRate")]
    public int BudgetUsageRate { get; set; }


    // =========================
    // AI 주간 분석
    // =========================

    [JsonProperty("aiSummary")]
    public string AiSummary { get; set; }

    [JsonProperty("goodPoint")]
    public string GoodPoint { get; set; }

    [JsonProperty("improvement")]
    public string Improvement { get; set; }

    [JsonProperty("nextWeekSuggestion")]
    public string NextWeekSuggestion { get; set; }
}