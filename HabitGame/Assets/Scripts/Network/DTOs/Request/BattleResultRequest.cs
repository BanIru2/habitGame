using Newtonsoft.Json;

/// <summary>
/// PvP 배틀 결과 제출을 위한 요청 DTO
/// </summary>
public class BattleResultRequest
{
    [JsonProperty("battleId")]
    public string BattleId { get; set; }

    // 상대 유저 식별 ID
    [JsonProperty("opponentUserId")]
    public long OpponentUserId { get; set; }

    // 승리 여부 (true: 승리, false: 패배)  << 무승부를 판정하지 못해 아래로 대체해야할 것으로 보임
    [JsonProperty("isWin")]
    public bool IsWin { get; set; }

    // 전투 결과 (WIN, DRWA, LOSE)
    [JsonProperty("result")]
    public string Result { get; set; }
}
