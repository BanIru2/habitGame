using System.Threading.Tasks;

public class AuthService
{
    private readonly ApiClient apiClient;

    public AuthService(ApiClient apiClient)
    {
        this.apiClient = apiClient;
    }

    // ·Î±×ÀÎ
    public Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        return apiClient.PostAsync<LoginRequest, LoginResponse>(
            "/auth/login",
            request
        );
    }
}
