using System;
using System.Threading.Tasks;

public class AuthService
{
    private readonly ApiClient apiClient;

    public AuthService(ApiClient apiClient)
    {
        this.apiClient = apiClient;
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        RegisterResponse response = await apiClient.PostAsync<RegisterRequest, RegisterResponse>(
            "/auth/register",
            request
        );

        if (response == null)
            throw new InvalidOperationException("회원가입 응답이 비어 있습니다.");

        if (response.Id <= 0)
            throw new InvalidOperationException("회원가입 응답에 유효한 사용자 ID가 없습니다.");

        return response;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        LoginResponse response = await apiClient.PostAsync<LoginRequest, LoginResponse>(
            "/auth/login",
            request
        );

        if (response == null)
            throw new InvalidOperationException("로그인 응답이 비어 있습니다.");

        if (response.UserId <= 0)
            throw new InvalidOperationException("로그인 응답에 유효한 사용자 ID가 없습니다.");

        if (string.IsNullOrWhiteSpace(response.AccessToken))
            throw new InvalidOperationException("로그인 응답에 Access Token이 없습니다.");

        apiClient.SetAccessToken(response.AccessToken);

        return response;
    }

    public async Task<MeResponse> GetMeAsync()
    {
        try
        {
            MeResponse response = await apiClient.GetAsync<MeResponse>("/auth/me");

            if (response == null)
                throw new InvalidOperationException("현재 사용자 응답이 비어 있습니다.");

            if (response.UserId <= 0)
                throw new InvalidOperationException("현재 사용자 응답에 유효한 사용자 ID가 없습니다.");

            return response;
        }
        catch (ApiException exception) when (exception.StatusCode == 401)
        {
            apiClient.SetAccessToken(null);
            throw;
        }
    }
}
