using System;
using System.Threading.Tasks;

public class WeeklyReportService
{
    private readonly ApiClient apiClient;

    public WeeklyReportService(ApiClient apiClient)
    {
        this.apiClient = apiClient;
    }

    public Task<WeeklyReportResponse> GetWeeklyReportAsync()
    {
        long userId = GetCurrentUserId();

        return apiClient.GetAsync<WeeklyReportResponse>(
            $"/weekly-report?userId={userId}"
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