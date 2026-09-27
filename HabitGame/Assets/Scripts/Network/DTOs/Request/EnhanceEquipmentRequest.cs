using System.Collections.Generic;
using Newtonsoft.Json;

public class EnhanceEquipmentRequest
{
    [JsonProperty("userId")]
    public long UserId { get; set; }

    // 강화 대상의 식별자
    [JsonProperty("targetInventoryId")]
    public long TargetInventoryId { get; set; }

    // 강화 재료 장비의 식별자 리스트 (한 번에 다중 선택 가능하도록)
    [JsonProperty("materialInventoryId")]
    public List<long> MaterialInventoryId { get; set; }
}
