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
        return apiClient.GetAsync<CharacterResponse>(
            $"/characters/me?userId={apiClient.CurrentUserId}"
        );
    }
}
