using Newtonsoft.Json;

/// <summary>
/// PvP 배틀 시작 요청 DTO
/// POST /battle/start
/// </summary>
public class BattleStartRequest
{
    [JsonProperty("userId")]
    public long UserId { get; set; }

    [JsonProperty("enemyUserId")]
    public long EnemyUserId { get; set; }
}