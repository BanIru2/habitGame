using Newtonsoft.Json;

/// <summary>
/// PvP 배틀 결과 리포트 응답 DTO
/// POST /battle/results
/// GET /battle/results?userId=
/// </summary>
public class BattleResultReportResponse
{
    [JsonProperty("battleId")]
    public string BattleId { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("isWin")]
    public bool IsWin { get; set; }

    [JsonProperty("result")]
    public string Result { get; set; }

    [JsonProperty("scoreBefore")]
    public int ScoreBefore { get; set; }

    [JsonProperty("scoreAfter")]
    public int ScoreAfter { get; set; }

    [JsonProperty("scoreDelta")]
    public int ScoreDelta { get; set; }

    [JsonProperty("rankBefore")]
    public int RankBefore { get; set; }

    [JsonProperty("rankAfter")]
    public int RankAfter { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }
}
