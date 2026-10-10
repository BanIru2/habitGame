
using Newtonsoft.Json;

/// <summary>
/// 생활습관 연속 달성(Streak) 보상 수령 결과 DTO
///
/// 서버에서 실제 보상 지급 여부와
/// 지급된 아이템 정보를 전달받음
/// </summary>
public class StreakRewardResponse
{
    // 이번 요청으로 보상이 새롭게 지급되었는지
    [JsonProperty("claimed")]
    public bool Claimed { get; set; }

    // 해당 마일스톤 보상을 이전에 이미 수령했는지
    [JsonProperty("alreadyClaimed")]
    public bool AlreadyClaimed { get; set; }

    // 보상을 받은 생활습관 목표 ID
    [JsonProperty("goalId")]
    public long GoalId { get; set; }

    // 보상 기준에 도달한 연속 달성 횟수
    // 예: Daily 10일, Weekly 2주
    [JsonProperty("milestone")]
    public int Milestone { get; set; }

    // 현재 서버에 저장된 연속 달성 횟수
    [JsonProperty("streakCount")]
    public int StreakCount { get; set; }

    // 목표 주기: DAILY 또는 WEEKLY
    [JsonProperty("period")]
    public string Period { get; set; }

    // 지급된 특성 탐색권의 아이템 ID
    [JsonProperty("itemId")]
    public string ItemId { get; set; }

    // 지급된 아이템 수량 (현재 서버 기준 1개)
    [JsonProperty("quantity")]
    public int Quantity { get; set; }
}
