using System.Threading.Tasks;

public class CharacterService
{
    private readonly ApiClient apiClient;

    public CharacterService(ApiClient apiClient)
    {
        this.apiClient = apiClient;
    }

    // 캐릭터 정보 조회
    public Task<CharacterResponse> GetMyCharacterAsync()
    {
        return apiClient.GetAsync<CharacterResponse>("/characters/me");
    }
}
