using Newtonsoft.Json;

/// <summary>
/// 상점 아이템 구매 요청을 위한 DTO
/// </summary>
public class PurchaseItemRequest
{

    [JsonProperty("userId")]
    public long UserId { get; set; }

    // 구매하려는 아이템의 고유 ID
    [JsonProperty("itemId")]
    public string ItemId { get; set; }

}