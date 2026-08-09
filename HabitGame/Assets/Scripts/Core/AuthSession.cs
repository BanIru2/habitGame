using System;

/// <summary>
/// Holds refresh-token state for the lifetime of the running application only.
/// </summary>
public static class AuthSession
{
    public static string RefreshToken { get; private set; }

    public static void SetRefreshToken(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new ArgumentException("A refresh token is required.", nameof(refreshToken));

        RefreshToken = refreshToken;
    }

    public static void ClearRefreshToken()
    {
        RefreshToken = null;
    }
}
