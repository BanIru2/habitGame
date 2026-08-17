using System;
using UnityEngine;

/// <summary>
/// Persists the refresh token between application launches.
/// Access tokens must remain in memory and must not be stored here.
/// </summary>
public static class TokenStorage
{
    private const string RefreshTokenKey = "HabitPVP.Auth.RefreshToken";

    public static void SaveRefreshToken(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new ArgumentException("A refresh token is required.", nameof(refreshToken));

        PlayerPrefs.SetString(RefreshTokenKey, refreshToken);
        PlayerPrefs.Save();
    }

    public static string LoadRefreshToken()
    {
        return PlayerPrefs.GetString(RefreshTokenKey, string.Empty);
    }

    public static bool HasRefreshToken()
    {
        return PlayerPrefs.HasKey(RefreshTokenKey)
            && !string.IsNullOrWhiteSpace(LoadRefreshToken());
    }

    public static void ClearRefreshToken()
    {
        if (!PlayerPrefs.HasKey(RefreshTokenKey))
            return;

        PlayerPrefs.DeleteKey(RefreshTokenKey);
        PlayerPrefs.Save();
    }
}
