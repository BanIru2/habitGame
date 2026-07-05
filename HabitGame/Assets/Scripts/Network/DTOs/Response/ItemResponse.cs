using Newtonsoft.Json;

public class ItemResponse
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("itemType")]
    public string ItemType { get; set; }

    [JsonProperty("slotType")]
    public string SlotType { get; set; }

    [JsonProperty("price")]
    public int Price { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("requiredAttributeType")]
    public string RequiredAttributeType { get; set; }

    [JsonProperty("requiredAttributeLevel")]
    public int RequiredAttributeLevel { get; set; }

    [JsonProperty("atkBonus")]
    public int AtkBonus { get; set; }

    [JsonProperty("defBonus")]
    public int DefBonus { get; set; }

    [JsonProperty("hpBonus")]
    public int HpBonus { get; set; }

    [JsonProperty("spdBonus")]
    public int SpdBonus { get; set; }

    [JsonProperty("critBonus")]
    public double CritBonus { get; set; }
}