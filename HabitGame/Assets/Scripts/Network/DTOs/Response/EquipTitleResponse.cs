using Newtonsoft.Json;

public class EquipTitleResponse
{
    // 변경 후 최종 장착된 칭호 ID (해제 시 null)
    [JsonProperty("equippedTitleId")]
    public string EquippedTitleId { get; set; }
}
