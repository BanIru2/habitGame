using Newtonsoft.Json;

/// <summary>
/// 전투 결과를 화면에 보여주기 위한 응답 DTO
/// </summary>
public class BattleResultResponse
{
    [JsonProperty("isWin")]
    public bool IsWin { get; set; }    // request와 마찬가지 무승부 결과를 저장하지 못해 아래로 변경해야할 것

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
}