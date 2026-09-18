using System.Collections.Generic;
using Newtonsoft.Json;

public class GachaResponse
{
    // 차감 후 유저의 최종 잔여 골드
    [JsonProperty("remainingGold")]
    public int RemainingGold { get; set; }
}