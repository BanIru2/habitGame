using Newtonsoft.Json;
using UnityEngine;

public class UseItemResponse
{
    [JsonProperty("inventoryId")]
    public long InventoryId { get; set; }

    [JsonProperty("itemId")]
    public string ItemId { get; set; }

    [JsonProperty("quantity")]
    public int Quantity { get; set; }
}
