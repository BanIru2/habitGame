using System;
using UnityEngine;

/// <summary>
/// Provides a Unity UI Button entry point for the authentication logout flow.
/// </summary>
[DisallowMultipleComponent]
public sealed class LogoutUIController : MonoBehaviour
{
    private bool logoutInProgress;

    public async void Logout()
    {
        if (logoutInProgress)
            return;

        logoutInProgress = true;

        try
        {
            ServiceRegistry registry = ServiceRegistry.Instance;
            if (registry == null || registry.Auth == null)
                throw new InvalidOperationException("인증 서비스를 사용할 수 없습니다.");

            await registry.Auth.LogoutAsync();
        }
        catch (Exception exception)
        {
            Debug.LogError($"[Auth] Logout flow failed: {exception.Message}", this);
        }
        finally
        {
            logoutInProgress = false;
        }
    }
}
