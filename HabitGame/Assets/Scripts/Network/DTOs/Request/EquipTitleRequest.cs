using Newtonsoft.Json;

public class EquipTitleRequest
{
    [JsonProperty("userId")]
    public long UserId { get; set; }

    // 장착할 칭호 ID (해제 시 null)
    [JsonProperty("titleId")]
    public string TitleId { get; set; }
}
