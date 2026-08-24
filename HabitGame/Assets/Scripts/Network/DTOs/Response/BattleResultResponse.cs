using Newtonsoft.Json;

/// <summary>
/// 전투 결과를 화면에 보여주기 위한 응답 DTO
/// </summary>
public class BattleResultResponse
{
    [JsonProperty("battleId")]
    public string BattleId { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("result")]
    public string Result { get; set; }

    [JsonProperty("scoreBefore")]
    public int ScoreBefore { get; set; }

    [JsonProperty("scoreDelta")]
    public int ScoreDelta { get; set; }

}
