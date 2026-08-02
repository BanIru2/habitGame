using System;
using UnityEngine;

/// <summary>
/// 임시 개발용 자체 인증 통합 테스트 컴포넌트입니다.
/// Play Mode에서 Inspector 값을 입력한 뒤 ContextMenu 명령을 실행합니다.
/// </summary>
[DisallowMultipleComponent]
public class AuthIntegrationTest : MonoBehaviour
{
    [Header("Local Auth Test Account")]
    [SerializeField] private string email = string.Empty;
    [SerializeField] private string password = string.Empty;
    [SerializeField] private string nickname = string.Empty;

    [ContextMenu("Auth Test/Register")]
    private async void Register()
    {
        if (!CanSendRequest("Register") || !ValidateCredentials(requireNickname: true))
            return;

        try
        {
            RegisterResponse response = await ServiceRegistry.Instance.Auth.RegisterAsync(
                new RegisterRequest
                {
                    Email = email.Trim(),
                    Password = password,
                    Nickname = nickname.Trim()
                }
            );

            UserSession.SetUser(response);
            LogSuccess("Register");
        }
        catch (Exception exception)
        {
            LogFailure("Register", exception);
        }
    }

    [ContextMenu("Auth Test/Login")]
    private async void Login()
    {
        if (!CanSendRequest("Login") || !ValidateCredentials(requireNickname: false))
            return;

        try
        {
            LoginResponse response = await ServiceRegistry.Instance.Auth.LoginAsync(
                new LoginRequest
                {
                    Email = email.Trim(),
                    Password = password
                }
            );

            UserSession.SetUser(response);
            LogSuccess("Login");
        }
        catch (Exception exception)
        {
            LogFailure("Login", exception);
        }
    }

    [ContextMenu("Auth Test/Print Current Session")]
    private void PrintCurrentSession()
    {
        Debug.Log(
            $"[AUTH TEST][Session] loggedIn={UserSession.IsLoggedIn}, " +
            $"userId={UserSession.UserId}, nickname={UserSession.Nickname}",
            this
        );
    }

    [ContextMenu("Auth Test/Restore Session")]
    private void RestoreSession()
    {
        bool restored = UserSession.TryRestore();
        Debug.Log(
            $"[AUTH TEST][Restore] success={restored}, " +
            $"userId={UserSession.UserId}, nickname={UserSession.Nickname}",
            this
        );
    }

    [ContextMenu("Auth Test/Logout")]
    private void Logout()
    {
        UserSession.Logout();
        Debug.Log("[AUTH TEST][Logout] success=true, userId=0, nickname=", this);
    }

    private bool CanSendRequest(string operation)
    {
        if (Application.isPlaying)
            return true;

        Debug.LogWarning($"[AUTH TEST][{operation}] Play Mode에서 실행해야 합니다.", this);
        return false;
    }

    private bool ValidateCredentials(bool requireNickname)
    {
        bool valid = !string.IsNullOrWhiteSpace(email)
                     && !string.IsNullOrWhiteSpace(password)
                     && (!requireNickname || !string.IsNullOrWhiteSpace(nickname));

        if (!valid)
            Debug.LogWarning("[AUTH TEST] Inspector에 필요한 계정 값을 입력하세요.", this);

        return valid;
    }

    private void LogSuccess(string operation)
    {
        Debug.Log(
            $"[AUTH TEST][{operation}] success=true, " +
            $"userId={UserSession.UserId}, nickname={UserSession.Nickname}",
            this
        );
    }

    private void LogFailure(string operation, Exception exception)
    {
        Debug.LogError(
            $"[AUTH TEST][{operation}] success=false, " +
            $"userId=0, nickname=, message={exception.Message}",
            this
        );
    }
}
