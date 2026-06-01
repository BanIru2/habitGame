using Newtonsoft.Json;
using System.Collections.Generic;

/// <summary>
/// 시즌 랭킹 정보를 담은 응답 DTO
/// </summary>
public class RankingListResponse
{
    // 현재 시즌 정보
    [JsonProperty("season")]
    public int Season { get; set; }

    // 랭킹 정보 리스트
    [JsonProperty("rankings")]
    public List<RankingEntryResponse> Rankings { get; set; }

    // 사용자 자신의 랭킹 정보
    [JsonProperty("myRanking")]
    public RankingEntryResponse MyRanking { get; set; }

    // 서버에서 마지막으로 갱신된 시간
    [JsonProperty("updatedAt")]
    public string UpdatedAt { get; set; }
}