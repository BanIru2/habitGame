using System.Collections.Generic;
using Newtonsoft.Json;

public class GachaRequest
{
    [JsonProperty("userId")]
    public long UserId { get; set; }

    // 뽑기 횟수 (1 또는 10)
    [JsonProperty("count")]
    public int Count { get; set; }

    // 추첨된 장비 식별자 목록 (ItemDataSO.itemId)
    [JsonProperty("itemIds")]
    public List<string> ItemIds { get; set; }
}