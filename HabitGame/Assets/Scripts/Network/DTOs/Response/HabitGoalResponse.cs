using Newtonsoft.Json;

/// <summary>
/// 습관 목표 정보를 담은 응답 DTO
/// </summary>
public class HabitGoalResponse
{
    // 목표의 고유 ID
    [JsonProperty("id")]
    public long Id { get; set; }

    // 유저 식별 ID
    [JsonProperty("userId")]
    public long UserId { get; set; }

    // 습관 카테고리 (신체, 자기개발, 환경, 바이오리듬 등)
    [JsonProperty("category")]
    public string Category { get; set; }

    // 목표로 하는 수치
    [JsonProperty("targetAmount")]
    public float TargetAmount { get; set; }

    // 수치 단위 (분, 회, 페이지 등)
    [JsonProperty("unit")]
    public string Unit { get; set; }

    // 목표 기간 (DAILY, WEEKLY 등)
    [JsonProperty("period")]
    public string Period { get; set; }

    // 현재 연속 달성 횟수(스트릭)
    [JsonProperty("streakCount")]
    public int StreakCount { get; set; }

    // 현재 활성화 여부
    [JsonProperty("isActive")]
    public bool IsActive { get; set; }

    // 목표 생성 일시
    [JsonProperty("createdAt")]
    public string CreatedAt { get; set; }
}