
using Newtonsoft.Json;

/// <summary>
/// 생활습관 연속 달성(Streak) 보상 수령 요청 DTO
/// 
/// Daily: 10일, 20일, 30일...
/// Weekly: 2주, 4주, 6주...
/// 
/// POST /rewards/streak/claim
/// </summary>
public class ClaimStreakRewardRequest
{
    // 보상을 받을 생활습관 목표의 고유 ID
    // 사용자 ID는 서버에서 JWT 인증 정보로 확인하므로 필요 없음
    [JsonProperty("goalId")]
    public long GoalId { get; set; }
}
