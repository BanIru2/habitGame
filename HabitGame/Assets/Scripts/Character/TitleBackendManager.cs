using System.Threading.Tasks;
using UnityEngine;

public class TitleBackendManager : MonoBehaviour
{
    // 보유 칭호 목록 및 착용 정보 조회 요청
    public async Task<UserTitlesResponse> FetchTitlesAsync()
    {
        return await ServiceRegistry.Instance.Title.GetMyTitlesAsync();
    }

    // 칭호 장착 / 해제 요청
    public async Task<EquipTitleResponse> EquipTitleAsync(string titleId)
    {
        var request = new EquipTitleRequest
        {
            UserId = ApiClient.Instance.CurrentUserId,
            TitleId = titleId
        };

        return await ServiceRegistry.Instance.Title.EquipTitleAsync(request);
    }
}
