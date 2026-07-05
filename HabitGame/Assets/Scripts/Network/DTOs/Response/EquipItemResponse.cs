using Newtonsoft.Json;

public class EquipItemResponse
{
    [JsonProperty("inventoryId")]
    public long InventoryId { get; set; }

    [JsonProperty("itemId")]
    public string ItemId { get; set; }

    [JsonProperty("slotType")]
    public string SlotType { get; set; }

    [JsonProperty("isEquipped")]
    public bool IsEquipped { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }
}