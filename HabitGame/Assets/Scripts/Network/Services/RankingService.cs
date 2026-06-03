using System.Threading.Tasks;

public class RankingService
{
    private readonly ApiClient apiClient;

    public RankingService(ApiClient apiClient)
    {
        this.apiClient = apiClient;
    }

    // ·©Å· Á¶È¸
    public Task<RankingListResponse> GetRankingsAsync()
    {
        return apiClient.GetAsync<RankingListResponse>("/rankings");
    }
}
