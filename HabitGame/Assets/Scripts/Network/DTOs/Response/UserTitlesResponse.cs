using System.Collections.Generic;
using Newtonsoft.Json;

public class UserTitlesResponse
{
    // 현재 장착 중인 칭호 ID (미장착 시 null)
    [JsonProperty("equippedTitleId")]
    public string EquippedTitleId { get; set; }

    // 유저가 보유한 칭호 ID 목록
    [JsonProperty("ownedTitleIds")]
    public List<string> OwnedTitleIds { get; set; } = new List<string>();
}
