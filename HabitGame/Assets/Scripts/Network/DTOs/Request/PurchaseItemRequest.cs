using Newtonsoft.Json;

/// <summary>
/// 상점 아이템 구매 요청을 위한 DTO
/// </summary>
public class PurchaseItemRequest
{
    // 구매하려는 아이템의 고유 ID
    [JsonProperty("itemId")]
    public string ItemId { get; set; }

    // 구매 수량
    [JsonProperty("quantity")]
    public int Quantity { get; set; }
}