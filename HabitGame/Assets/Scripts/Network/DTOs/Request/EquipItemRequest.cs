using Newtonsoft.Json;

/// <summary>
/// 캐릭터 장비 장착 또는 해제 요청을 위한 DTO
/// </summary>
public class EquipItemRequest
{
    // 장착/해제할 인벤토리 아이템의 고유 ID
    [JsonProperty("inventoryId")]
    public long InventoryId { get; set; }

    // 장착 여부
    [JsonProperty("isEquipped")]
    public bool IsEquipped { get; set; }
}