using Newtonsoft.Json;

/// <summary>
/// 랭킹 정보 한줄을 담은 응답 DTO
/// </summary>
public class RankingEntryResponse
{
    [JsonProperty("id")]
    public int RankingId { get; set; }

    // 유저 식별 ID
    [JsonProperty("userId")]
    public long UserId { get; set; }

    // 현재 시즌 획득 점수
    [JsonProperty("rating")]
    public int Score { get; set; }

    [JsonProperty("wins")]
    public int Wins { get; set; }

    [JsonProperty("losses")]
    public int Losses { get; set; }

    // 현재 순위
    [JsonProperty("rank")]
    public int Rank { get; set; }
}