using Newtonsoft.Json;

/// <summary>
/// 새로운 습관 목표 생성을 위한 DTO
/// </summary>
public class CreateHabitGoalRequest
{
    // 습관 카테고리 (신체, 자기개발, 환경, 바이오리듬)
    [JsonProperty("category")]
    public string Category { get; set; }

    // 목표 수치
    [JsonProperty("targetAmount")]
    public float TargetAmount { get; set; }

    // 수치 단위 (분, 회, 페이지 등)
    [JsonProperty("unit")]
    public string Unit { get; set; }

    // 목표 기간 (DAILY, WEEKLY 등)
    [JsonProperty("period")]
    public string Period { get; set; }
}