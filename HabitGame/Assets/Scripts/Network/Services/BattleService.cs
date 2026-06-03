using System.Threading.Tasks;

public class BattleService
{
    private readonly ApiClient apiClient;

    public BattleService(ApiClient apiClient)
    {
        this.apiClient = apiClient;
    }

    // PvP 결과 제출
    public Task<BattleResultResponse> SubmitResultAsync(BattleResultRequest request)
    {
        return apiClient.PostAsync<BattleResultRequest, BattleResultResponse>(
            "/battle/result",
            request
        );
    }
}
