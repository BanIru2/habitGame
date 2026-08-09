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

        if (string.IsNullOrWhiteSpace(response.RefreshToken))
            throw new InvalidOperationException("Login response does not contain a refresh token.");

        apiClient.SetAccessToken(response.AccessToken);
        AuthSession.SetRefreshToken(response.RefreshToken);

        return response;
    }

    public async Task<RefreshResponse> RefreshAsync()
    {
        string currentRefreshToken = AuthSession.RefreshToken;
        if (string.IsNullOrWhiteSpace(currentRefreshToken))
            throw new InvalidOperationException("A refresh token is required to refresh authentication.");

        try
        {
            RefreshResponse response = await apiClient.PostAsync<RefreshTokenRequest, RefreshResponse>(
                "/auth/refresh",
                new RefreshTokenRequest { RefreshToken = currentRefreshToken }
            );

            if (response == null)
            {
                ClearLocalSession();
                throw new InvalidOperationException("Refresh response is empty.");
            }

            if (string.IsNullOrWhiteSpace(response.AccessToken))
            {
                ClearLocalSession();
                throw new InvalidOperationException("Refresh response does not contain an access token.");
            }

            if (string.IsNullOrWhiteSpace(response.RefreshToken))
            {
                ClearLocalSession();
                throw new InvalidOperationException("Refresh response does not contain a refresh token.");
            }

            apiClient.SetAccessToken(response.AccessToken);
            AuthSession.SetRefreshToken(response.RefreshToken);

            return response;
        }
        catch (ApiException exception) when (exception.StatusCode == 401)
        {
            ClearLocalSession();
            throw;
        }
    }

    public async Task LogoutAsync()
    {
        string currentRefreshToken = AuthSession.RefreshToken;

        try
        {
            if (!string.IsNullOrWhiteSpace(currentRefreshToken))
            {
                await apiClient.PostAsync<RefreshTokenRequest, object>(
                    "/auth/logout",
                    new RefreshTokenRequest { RefreshToken = currentRefreshToken }
                );
            }
        }
        finally
        {
            ClearLocalSession();
        }
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

    private void ClearLocalSession()
    {
        apiClient.SetAccessToken(null);
        AuthSession.ClearRefreshToken();
        UserSession.Logout();
    }
}
