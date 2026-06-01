using Newtonsoft.Json;

/// <summary>
/// 상점 아이템 구매 로그 정보를 담은 응답 DTO
/// </summary>
public class PurchaseLogResponse
{
    // 로그의 고유 ID
    [JsonProperty("id")]
    public long Id { get; set; }

    // 유저 식별 ID
    [JsonProperty("userId")]
    public long UserId { get; set; }

    // 구매한 아이템 ID
    [JsonProperty("itemId")]
    public string ItemId { get; set; }

    // 소모된 골드 양
    [JsonProperty("goldSpent")]
    public int GoldSpent { get; set; }

    // 구매 일시
    [JsonProperty("purchasedAt")]
    public string PurchasedAt { get; set; }
}