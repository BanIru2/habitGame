using Newtonsoft.Json;

/// <summary>
/// PvP 배틀 결과 리포트 응답 DTO
/// POST /battle/results
/// GET /battle/results?userId=
/// </summary>
public class BattleResultReportResponse
{
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("userId")]
    public long UserId { get; set; }

    [JsonProperty("enemyUserId")]
    public long EnemyUserId { get; set; }

    [JsonProperty("result")]
    public string Result { get; set; }

    [JsonProperty("myPower")]
    public double MyPower { get; set; }

    [JsonProperty("enemyPower")]
    public double EnemyPower { get; set; }

    [JsonProperty("gainedExp")]
    public int GainedExp { get; set; }

    [JsonProperty("gainedGold")]
    public int GainedGold { get; set; }

    [JsonProperty("createdAt")]
    public string CreatedAt { get; set; }
}