using System;
using System.Threading.Tasks;

public class CharacterService
{
    private readonly ApiClient apiClient;

    public CharacterService(ApiClient apiClient)
    {
        this.apiClient = apiClient;
    }

    // ĳ���� ���� ��ȸ
    public Task<CharacterResponse> GetMyCharacterAsync()
    {
        long userId = GetCurrentUserId();

        return apiClient.GetAsync<CharacterResponse>(
            $"/characters/me?userId={userId}"
        );
    }

    private long GetCurrentUserId()
    {
        long userId = apiClient.CurrentUserId;
        if (userId <= 0)
            throw new InvalidOperationException("로그인이 필요합니다.");

        return userId;
    }
}
