using Newtonsoft.Json;
using UnityEngine;

public class UseItemResponse
{
    [JsonProperty("inventoryId")]
    public long InventoryId { get; set; }

    [JsonProperty("itemId")]
    public string ItemId { get; set; }

    [JsonProperty("itemType")]
    public string ItemType { get; set; }

    [JsonProperty("remainingQuantity")]
    public int Quantity { get; set; }

    [JsonProperty("isDeleted")]
    public bool IsDeleted { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }
}
