using Newtonsoft.Json;

public class EquipItemRequest
{
    [JsonProperty("userId")]
    public long UserId { get; set; }

    [JsonProperty("inventoryId")]
    public long InventoryId { get; set; }
}