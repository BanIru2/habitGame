using Newtonsoft.Json;

public class PurchaseItemResponse
{
    [JsonProperty("purchaseStatus")]
    public string PurchaseStatus { get; set; }

    [JsonProperty("itemName")]
    public string ItemName { get; set; }

    [JsonProperty("remainingGold")]
    public int RemainingGold { get; set; }

    [JsonProperty("inventoryId")]
    public long InventoryId { get; set; }

}

