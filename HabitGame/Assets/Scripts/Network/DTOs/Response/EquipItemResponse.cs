using Newtonsoft.Json;
using System.Collections.Generic;

public class EquipItemResponse
{
    [JsonProperty("inventoryItems")]
    public List<InventoryItemResponse> InventoryItems { get; set; }
}