using Newtonsoft.Json;

public class UseItemRequest
{
    [JsonProperty("userId")]
    public long UserId { get; set; }

    [JsonProperty("inventoryId")]
    public long InventoryId { get; set; }
}
