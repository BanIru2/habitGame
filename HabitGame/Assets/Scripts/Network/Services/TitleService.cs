using System;
using System.Threading.Tasks;

public class TitleService
{
    private readonly ApiClient apiClient;

    public TitleService(ApiClient apiClient)
    {
        this.apiClient = apiClient;
    }

    private long GetCurrentUserId()
    {
        long userId = apiClient.CurrentUserId;
        if (userId <= 0)
            throw new InvalidOperationException("로그인이 필요합니다.");

        return userId;
    }

    // 내 보유 칭호 목록 및 착용 중인 칭호 조회
    public Task<UserTitlesResponse> GetMyTitlesAsync()
    {
        long userId = GetCurrentUserId();

        return apiClient.GetAsync<UserTitlesResponse>(
            $"/titles/me?userId={userId}"
        );
    }

    // 칭호 장착 / 해제 요청 (TitleId가 null이면 해제)
    public Task<EquipTitleResponse> EquipTitleAsync(EquipTitleRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        request.UserId = GetCurrentUserId();

        return apiClient.PostAsync<EquipTitleRequest, EquipTitleResponse>(
            "/titles/equip",
            request
        );
    }
}
