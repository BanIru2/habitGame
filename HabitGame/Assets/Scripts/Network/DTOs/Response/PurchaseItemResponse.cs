using Newtonsoft.Json;

public class PurchaseItemResponse
{
    [JsonProperty("message")]
    public string Message { get; set; }

    [JsonProperty("userId")]
    public long UserId { get; set; }

    [JsonProperty("itemId")]
    public string ItemId { get; set; }

    [JsonProperty("itemName")]
    public string ItemName { get; set; }

    [JsonProperty("itemType")]
    public string ItemType { get; set; }

    [JsonProperty("slotType")]
    public string SlotType { get; set; }

    [JsonProperty("price")]
    public int Price { get; set; }

    [JsonProperty("remainingGold")]
    public int RemainingGold { get; set; }

    [JsonProperty("inventoryId")]
    public long InventoryId { get; set; }

    [JsonProperty("purchaseLogId")]
    public long PurchaseLogId { get; set; }
}