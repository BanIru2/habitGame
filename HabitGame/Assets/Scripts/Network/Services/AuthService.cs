using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

public class AuthService
{
    private readonly ApiClient apiClient;

    public AuthService(ApiClient apiClient)
    {
        this.apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        this.apiClient.ConfigureAuthentication(RefreshAccessTokenAsync, ClearLocalSession);
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
        TokenStorage.SaveRefreshToken(response.RefreshToken);

        return response;
    }

    public async Task<RefreshResponse> RefreshAsync()
    {
        string currentRefreshToken = AuthSession.RefreshToken;
        if (string.IsNullOrWhiteSpace(currentRefreshToken))
        {
            ClearLocalSession();
            throw new InvalidOperationException("A refresh token is required to refresh authentication.");
        }

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

            apiClient.SetAccessToken(response.AccessToken);

            if (!string.IsNullOrWhiteSpace(response.RefreshToken))
            {
                AuthSession.SetRefreshToken(response.RefreshToken);
                TokenStorage.SaveRefreshToken(response.RefreshToken);
            }

            return response;
        }
        catch (ApiException exception) when (
            exception.StatusCode == 400
            || exception.StatusCode == 401
            || exception.StatusCode == 403)
        {
            ClearLocalSession();
            throw;
        }
        catch (JsonException exception)
        {
            ClearLocalSession();
            throw new InvalidOperationException("Refresh response is invalid.", exception);
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

                Debug.Log("[Auth] Server logout succeeded.");
            }
            else
            {
                Debug.LogWarning("[Auth] Server logout skipped because no refresh token is available.");
            }
        }
        catch (ApiException exception)
        {
            Debug.LogWarning(
                $"[Auth] Server logout failed ({exception.StatusCode}): {exception.Message}"
            );
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[Auth] Server logout failed: {exception.Message}");
        }
        finally
        {
            ClearLocalSession();
            apiClient.ReturnToLoginScene();
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

    public void ClearLocalSession()
    {
        ClearRuntimeSession();
        TokenStorage.ClearRefreshToken();
    }

    public void ClearRuntimeSession()
    {
        apiClient.SetAccessToken(null);
        AuthSession.ClearRefreshToken();
        UserSession.Logout();
    }

    private async Task RefreshAccessTokenAsync()
    {
        await RefreshAsync();
    }
}
