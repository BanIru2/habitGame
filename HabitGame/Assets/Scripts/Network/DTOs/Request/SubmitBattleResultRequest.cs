using Newtonsoft.Json;

/// <summary>
/// PvP 배틀 결과 제출을 위한 요청 DTO
/// </summary>
public class SubmitBattleResultRequest
{
    [JsonProperty("battleId")]
    public string BattleId { get; set; }

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

    // BattleBackendManager 호환용. Backend 요청에는 전송하지 않는다.
    [JsonIgnore]
    public int GainedExp { get; set; }

    // BattleBackendManager 호환용. Backend 요청에는 전송하지 않는다.
    [JsonIgnore]
    public int GainedGold { get; set; }
}
