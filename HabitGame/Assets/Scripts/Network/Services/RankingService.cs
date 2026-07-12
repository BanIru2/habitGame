using System.Collections.Generic;
using System.Threading.Tasks;

public class RankingService
{
    private readonly ApiClient apiClient;

    public RankingService(ApiClient apiClient)
    {
        this.apiClient = apiClient;
    }

    // ·©Å· Á¶È¸
    public Task<List<RankingEntryResponse>> GetRankingsAsync()
    {
        return apiClient.GetAsync<List<RankingEntryResponse>>("/rankings");
    }
}
