using Newtonsoft.Json;

/// <summary>
/// PvP 배틀 시작 응답 DTO
/// POST /battle/start
/// </summary>
public class BattleStartResponse
{
    [JsonProperty("battleLogId")]
    public long BattleLogId { get; set; }

    [JsonProperty("userId")]
    public long UserId { get; set; }

    [JsonProperty("enemyUserId")]
    public long EnemyUserId { get; set; }

    [JsonProperty("myPower")]
    public double MyPower { get; set; }

    [JsonProperty("enemyPower")]
    public double EnemyPower { get; set; }

    [JsonProperty("result")]
    public string Result { get; set; }
}