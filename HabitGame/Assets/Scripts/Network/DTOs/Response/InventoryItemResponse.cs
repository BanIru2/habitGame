using Newtonsoft.Json;

/// <summary>
/// 인벤토리에 보유 중인 아이템 정보를 담은 응답 DTO
/// </summary>
public class InventoryItemResponse
{
    // 인벤토리 항목의 고유 ID
    [JsonProperty("id")]
    public long Id { get; set; }

    // 유저 식별 ID
    [JsonProperty("userId")]
    public long UserId { get; set; }

    // 아이템 데이터 ID (ScriptableObject와 매칭)
    [JsonProperty("itemId")]
    public string ItemId { get; set; }

    // 아이템 종류 (EQUIPMENT, CONSUMABLE 등)
    [JsonProperty("itemType")]
    public string ItemType { get; set; }

    // 보유 수량
    [JsonProperty("quantity")]
    public int Quantity { get; set; }

    // 장착 상태 (true: 장착 중)
    [JsonProperty("isEquipped")]
    public bool IsEquipped { get; set; }

    // 장착되는 슬롯 종류 (HEAD, BODY, WEAPON 등)
    [JsonProperty("slotType")]
    public string SlotType { get; set; }

    // 획득 일시
    [JsonProperty("acquiredAt")]
    public string AcquiredAt { get; set; }
}
