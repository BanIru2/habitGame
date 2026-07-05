using Newtonsoft.Json;

/// <summary>
/// PvP 배틀 결과 제출을 위한 요청 DTO
/// </summary>
public class BattleResultRequest
{
    // 상대 유저 식별 ID
    [JsonProperty("opponentUserId")]
    public long OpponentUserId { get; set; }

    // 승리 여부 (true: 승리, false: 패배)
    [JsonProperty("isWin")]
    public bool IsWin { get; set; }

    // 배틀 시 선택한 속성 (신체, 자기개발, 환경, 바이오리듬 등)
    [JsonProperty("selectedAttribute")]
    public string SelectedAttribute { get; set; }
}
