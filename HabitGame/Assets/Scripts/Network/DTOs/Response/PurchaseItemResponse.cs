using Newtonsoft.Json;

/// <summary>
/// 아이템 구매 정보 응답 DTO
/// </summary>
public class PurchaseItemResponse
{
    // 구매 기록
    [JsonProperty("purchaseLog")]
    public PurchaseLogResponse PurchaseLog { get; set; }

    // 구매로 새로 생긴 인벤토리 항목(아이템)
    [JsonProperty("newItem")]
    public InventoryItemResponse NewItem { get; set; }

    // 구매 후 남은 골드
    [JsonProperty("gold")]
    public int Gold { get; set; }
}